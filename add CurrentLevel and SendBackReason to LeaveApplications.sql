-- Adds columns needed for the multi-level leave approval workflow
-- (Level 1 = Reporting Manager, Level 2 = Department Head, Level 3 = HR).
--
-- Alternative (if you prefer EF migrations):
--   dotnet ef migrations add AddLeaveApprovalWorkflowLevels --project Infrastructure --startup-project API
--   dotnet ef database update --project Infrastructure --startup-project API

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.LeaveApplications')
      AND name = 'CurrentLevel'
)
BEGIN
    ALTER TABLE dbo.LeaveApplications
    ADD CurrentLevel INT NOT NULL DEFAULT 1;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.LeaveApplications')
      AND name = 'SendBackReason'
)
BEGIN
    ALTER TABLE dbo.LeaveApplications
    ADD SendBackReason NVARCHAR(500) NULL;
END
GO

-- Backfill: any leave application still Pending under the old single-step
-- model starts the new chain at Level 1 (already the column default, but
-- explicit for clarity/idempotency).
UPDATE dbo.LeaveApplications
SET CurrentLevel = 1
WHERE Status = 1 -- Pending
  AND CurrentLevel IS NULL;
GO
