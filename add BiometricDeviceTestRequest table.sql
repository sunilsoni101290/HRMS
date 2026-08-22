-- =====================================================================
-- BiometricDeviceTestRequest table - backs the real "Test Connection"
-- feature (BiometricDeviceService.RequestTestConnectionAsync /
-- GetTestConnectionResultAsync, BiometricAgentController's
-- pending-test-requests / test-result endpoints).
--
-- Needed because BiometricAgent only ever calls OUT to this API (poll
-- loop in Worker.cs) - there is no reverse channel for the API to reach
-- the agent synchronously. A Test Connection is therefore an async
-- request/response: this table holds the Pending row created when a user
-- clicks "Test Connection", which the assigned agent picks up on its next
-- poll cycle, actually attempts the ESSL SDK connection, and reports the
-- result back into this same row.
--
-- Run this against the ERP database, OR regenerate/apply the equivalent
-- via EF migrations (do not do both). Mirrors the changes already made to
-- Domain/Entities/BiometricDevice.cs and Infrastructure/ApplicationDbContext.cs.
-- Safe to re-run - every step is guarded.
-- =====================================================================

SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'BiometricDeviceTestRequests')
BEGIN
    CREATE TABLE [dbo].[BiometricDeviceTestRequests]
    (
        [Id]            nvarchar(450)   NOT NULL,
        [TenantId]      nvarchar(450)   NOT NULL,
        [DeviceId]      nvarchar(450)   NOT NULL,
        [AgentId]       nvarchar(450)   NULL,
        [Status]        int             NOT NULL CONSTRAINT [DF_BiometricDeviceTestRequests_Status] DEFAULT (1), -- 1=Pending
        [Stage]         nvarchar(50)    NULL,
        [Message]       nvarchar(500)   NULL,
        [DeviceInfo]    nvarchar(200)   NULL,
        [RequestedBy]   nvarchar(450)   NOT NULL,
        [RequestedOn]   datetime2       NOT NULL CONSTRAINT [DF_BiometricDeviceTestRequests_RequestedOn] DEFAULT (SYSUTCDATETIME()),
        [CompletedOn]   datetime2       NULL,
        [IsDeleted]     bit             NOT NULL CONSTRAINT [DF_BiometricDeviceTestRequests_IsDeleted] DEFAULT (0),
        [CreatedOn]     datetime2       NOT NULL CONSTRAINT [DF_BiometricDeviceTestRequests_CreatedOn] DEFAULT (SYSUTCDATETIME()),
        [CreatedBy]     nvarchar(max)   NOT NULL CONSTRAINT [DF_BiometricDeviceTestRequests_CreatedBy] DEFAULT (''),
        [ModifiedOn]    datetime2       NULL,
        [ModifiedBy]    nvarchar(max)   NULL,
        [IsActive]      bit             NOT NULL CONSTRAINT [DF_BiometricDeviceTestRequests_IsActive] DEFAULT (1),

        CONSTRAINT [PK_BiometricDeviceTestRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BiometricDeviceTestRequests_Tenants_TenantId]
            FOREIGN KEY ([TenantId]) REFERENCES [dbo].[Tenants] ([Id]),
        CONSTRAINT [FK_BiometricDeviceTestRequests_BiometricDevices_DeviceId]
            FOREIGN KEY ([DeviceId]) REFERENCES [dbo].[BiometricDevices] ([Id])
    );

    PRINT 'Created table BiometricDeviceTestRequests.';
END
ELSE
    PRINT 'BiometricDeviceTestRequests already exists - skipped.';
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BiometricDeviceTestRequests_Agent_Status'
      AND object_id = OBJECT_ID('dbo.BiometricDeviceTestRequests'))
BEGIN
    CREATE INDEX [IX_BiometricDeviceTestRequests_Agent_Status]
        ON [dbo].[BiometricDeviceTestRequests] ([AgentId], [Status]);

    PRINT 'Created index IX_BiometricDeviceTestRequests_Agent_Status.';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BiometricDeviceTestRequests_Device_RequestedOn'
      AND object_id = OBJECT_ID('dbo.BiometricDeviceTestRequests'))
BEGIN
    CREATE INDEX [IX_BiometricDeviceTestRequests_Device_RequestedOn]
        ON [dbo].[BiometricDeviceTestRequests] ([DeviceId], [RequestedOn]);

    PRINT 'Created index IX_BiometricDeviceTestRequests_Device_RequestedOn.';
END
GO

PRINT 'BiometricDeviceTestRequest table setup complete.';
