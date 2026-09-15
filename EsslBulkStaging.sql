/* ==========================================================================
   eSSL Attendance Bulk Staging - SQL Server objects
   ==========================================================================
   Adds a staging-table + stored-procedure bulk write path for
   BiometricAttendanceLogs, ADDITIVE to the existing EF AddRange/
   SaveChangesAsync path (which stays in EsslAttendanceSyncService.cs as an
   automatic fallback if this path throws for any reason - "correctness
   first, then performance").

   Nothing here touches Domain.Entities, AttendanceProcessorService, or any
   existing table's schema. Run this once against the HRMS application
   database (the same database BiometricAttendanceLogs lives in - NOT the
   eTimeTrackLite1 source database).

   Source identity: staging rows carry SourceTable + the raw eTimeTrackLite1
   DeviceLogId (SourceDeviceLogId, audit-only) plus the SAME
   DeviceTransactionId the C# side already computes via
   EsslAttendanceSyncService.BuildDeviceTransactionId - "ESSL-{DeviceLogId}"
   for the base table, "ESSL-{SourceTable}-{DeviceLogId}" for a monthly
   partition table. DeviceLogId is NEVER treated as globally unique here;
   DeviceTransactionId (built from SourceTable+DeviceLogId) is the one and
   only identity used for deduplication, matching
   BiometricAttendanceLogs' existing unique index.
   ========================================================================== */

-- ---------------------------------------------------------------------
-- 1. Staging table
-- ---------------------------------------------------------------------
-- One physical table shared by every run/tenant, scoped by BatchRunId so
-- concurrent tenants (or a retried run) can never see each other's rows.
-- Rows for a run are deleted by the stored procedure itself once that
-- run's merge has committed successfully - a failed run's rows are left
-- in place (not cleaned up) so they can be inspected for troubleshooting,
-- then cleared by the next successful run for the same BatchRunId or a
-- manual DELETE.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EsslDeviceLogStaging' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.EsslDeviceLogStaging
    (
        StagingId            BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        BatchRunId           UNIQUEIDENTIFIER     NOT NULL,
        TenantId              NVARCHAR(450)        NOT NULL,

        -- Pre-built BiometricAttendanceLogs row content. Id, EmployeeCode,
        -- PunchType, DeviceId, DeviceTransactionId, SourceTable,
        -- DownloadDate and CreatedBy are computed in C# EXACTLY as they are
        -- today (IDManager.GetNewId, ResolvePunchType, employee-mapping
        -- lookup, BuildDeviceTransactionId) - this table only stages the
        -- finished row, it never re-derives any of that business logic in
        -- T-SQL.
        Id                    NVARCHAR(450)        NOT NULL,
        EmployeeCode          NVARCHAR(100)        NOT NULL,
        PunchTime             DATETIME2(7)         NOT NULL,
        PunchType             INT                  NOT NULL,
        DeviceId              NVARCHAR(100)        NOT NULL,
        DeviceTransactionId   NVARCHAR(100)        NOT NULL,
        SourceTable           NVARCHAR(60)         NOT NULL,
        SourceDeviceLogId     INT                  NOT NULL,   -- audit only, NOT a uniqueness key
        DownloadDate          DATETIME2(7)         NULL,
        IsUnmapped            BIT                  NOT NULL CONSTRAINT DF_EsslDeviceLogStaging_IsUnmapped DEFAULT (0),
        CreatedBy             NVARCHAR(450)        NULL,
        StagedOn              DATETIME2(7)         NOT NULL CONSTRAINT DF_EsslDeviceLogStaging_StagedOn DEFAULT (SYSUTCDATETIME())
    );

    CREATE NONCLUSTERED INDEX IX_EsslDeviceLogStaging_BatchRunId
        ON dbo.EsslDeviceLogStaging (BatchRunId);
END
GO

-- ---------------------------------------------------------------------
-- 2. Bulk merge stored procedure
-- ---------------------------------------------------------------------
-- Takes everything already staged for @BatchRunId and, in one set-based
-- operation inside a transaction, inserts every row whose
-- DeviceTransactionId is not already present in BiometricAttendanceLogs.
-- Returns authoritative, DB-verified reconciliation counts as OUTPUT
-- parameters - the C# caller uses these (not its own running counters) as
-- the source of truth for RecordsImported/DuplicateCount/UnmappedCount for
-- whatever it staged in this call.
--
-- Reconciliation identity: @StagedCount = @InsertedCount + @DuplicateCount
-- always holds for a successful run (enforced by the COUNT arithmetic
-- below, not just asserted) - @FailedCount is only ever non-zero if the
-- INSERT itself throws, in which case the whole transaction rolls back and
-- the C# caller is expected to fall back to its existing EF insert path
-- for this batch instead of trusting partial staging counts.
CREATE OR ALTER PROCEDURE dbo.usp_EsslStaging_MergeAttendanceLogs
    @BatchRunId             UNIQUEIDENTIFIER,
    @StagedCount            INT OUTPUT,
    @InsertedCount          INT OUTPUT,
    @DuplicateCount         INT OUTPUT,
    @UnmappedInsertedCount  INT OUTPUT,
    @FailedCount            INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SELECT @StagedCount = 0, @InsertedCount = 0, @DuplicateCount = 0,
           @UnmappedInsertedCount = 0, @FailedCount = 0;

    SELECT @StagedCount = COUNT(*)
    FROM dbo.EsslDeviceLogStaging
    WHERE BatchRunId = @BatchRunId;

    IF @StagedCount = 0
        RETURN 0;

    DECLARE @InsertedIds TABLE (Id NVARCHAR(450) PRIMARY KEY);

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Set-based dedupe: a staged row is inserted only if no existing
        -- BiometricAttendanceLogs row already has the same
        -- DeviceTransactionId - the identical identity rule the existing
        -- C# pre-check and the table's own unique index already enforce,
        -- just evaluated once for the whole batch instead of per row.
        -- HOLDLOCK+UPDLOCK on the existence check is enough to be race-safe
        -- against a genuinely concurrent writer without needing to lock the
        -- whole target table.
        INSERT INTO dbo.BiometricAttendanceLogs
            (Id, TenantId, EmployeeCode, PunchTime, PunchType, DeviceId,
             DeviceTransactionId, VerifyMode, IsDuplicate, IsProcessed,
             SourceTable, DownloadDate, CreatedBy, CreatedOn)
        OUTPUT inserted.Id INTO @InsertedIds (Id)
        SELECT
            s.Id, s.TenantId, s.EmployeeCode, s.PunchTime, s.PunchType, s.DeviceId,
            s.DeviceTransactionId, NULL, 0, 0,
            s.SourceTable, s.DownloadDate, s.CreatedBy, SYSUTCDATETIME()
        FROM dbo.EsslDeviceLogStaging s
        WHERE s.BatchRunId = @BatchRunId
          AND NOT EXISTS (
                SELECT 1
                FROM dbo.BiometricAttendanceLogs b WITH (UPDLOCK, HOLDLOCK)
                WHERE b.DeviceTransactionId = s.DeviceTransactionId
              );

        SET @InsertedCount = @@ROWCOUNT;
        SET @DuplicateCount = @StagedCount - @InsertedCount;

        SELECT @UnmappedInsertedCount = COUNT(*)
        FROM dbo.EsslDeviceLogStaging s
        INNER JOIN @InsertedIds i ON i.Id = s.Id
        WHERE s.BatchRunId = @BatchRunId AND s.IsUnmapped = 1;

        -- Self-cleaning: only on a fully successful commit. A failed run's
        -- staged rows are deliberately left behind for inspection (see
        -- header comment) - they get cleared automatically the next time
        -- this same @BatchRunId succeeds, or can be deleted manually.
        DELETE FROM dbo.EsslDeviceLogStaging WHERE BatchRunId = @BatchRunId;

        COMMIT TRANSACTION;
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;

        -- Whole-batch failure (constraint violation, deadlock, etc.) - none
        -- of this batch was inserted. Report it as fully failed so the C#
        -- caller does NOT trust @InsertedCount/@DuplicateCount (both reset
        -- to 0 above are meaningless after a rollback) and instead falls
        -- back to its existing per-row EF insert path for this same batch,
        -- exactly like the current "bulk insert failed, falling back to
        -- row-by-row" branch already does for the EF path.
        SET @InsertedCount = 0;
        SET @DuplicateCount = 0;
        SET @UnmappedInsertedCount = 0;
        SET @FailedCount = @StagedCount;

        THROW;
    END CATCH
END
GO
