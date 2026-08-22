-- =====================================================================
-- BiometricAgent.BranchId / BiometricAgent.Description +
-- BiometricDevice.CommKey - closes the gap where the Agent master UI
-- couldn't record which branch an agent's machine sits at or a free-text
-- description, and where the Agent had no way to receive a device's SDK
-- comm password (CommKey) from the ERP (it was always hard-defaulted to 0
-- agent-side). Mirrors Domain/Entities/BiometricDevice.cs and
-- Infrastructure/ApplicationDbContext.cs. Safe to re-run.
-- =====================================================================

SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BiometricAgents') AND name = 'BranchId')
    ALTER TABLE [dbo].[BiometricAgents] ADD [BranchId] nvarchar(450) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BiometricAgents') AND name = 'Description')
    ALTER TABLE [dbo].[BiometricAgents] ADD [Description] nvarchar(500) NULL;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_BiometricAgents_Branches_BranchId')
BEGIN
    ALTER TABLE [dbo].[BiometricAgents]
        ADD CONSTRAINT [FK_BiometricAgents_Branches_BranchId]
        FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches] ([Id]);

    PRINT 'Added FK_BiometricAgents_Branches_BranchId.';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BiometricAgents_BranchId'
      AND object_id = OBJECT_ID('dbo.BiometricAgents'))
BEGIN
    CREATE INDEX [IX_BiometricAgents_BranchId] ON [dbo].[BiometricAgents] ([BranchId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BiometricDevices') AND name = 'CommKey')
    ALTER TABLE [dbo].[BiometricDevices] ADD [CommKey] int NOT NULL CONSTRAINT [DF_BiometricDevices_CommKey] DEFAULT (0);
GO

PRINT 'BiometricAgent Branch/Description + BiometricDevice CommKey columns ready.';
