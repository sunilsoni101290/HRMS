-- Adds ModuleName/FeatureName categorization columns to the existing
-- ErrorLogs table (see Domain/Entities/ErrorLog.cs), plus a couple of
-- indexes used by the Error Log management screen's filters.
-- Safe to run multiple times / on a fresh database where ErrorLogs was
-- just created by the InitialMigration.

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ErrorLogs'
)
BEGIN
    RAISERROR('ErrorLogs table does not exist - run the InitialMigration (or apply EF migrations) before this script.', 16, 1);
    RETURN;
END

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'ErrorLogs' AND COLUMN_NAME = 'ModuleName'
)
BEGIN
    ALTER TABLE dbo.ErrorLogs ADD ModuleName NVARCHAR(150) NULL;
END

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'ErrorLogs' AND COLUMN_NAME = 'FeatureName'
)
BEGIN
    ALTER TABLE dbo.ErrorLogs ADD FeatureName NVARCHAR(150) NULL;
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = 'IX_ErrorLogs_ModuleName' AND object_id = OBJECT_ID('dbo.ErrorLogs')
)
BEGIN
    CREATE INDEX IX_ErrorLogs_ModuleName ON dbo.ErrorLogs (ModuleName);
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = 'IX_ErrorLogs_IsResolved' AND object_id = OBJECT_ID('dbo.ErrorLogs')
)
BEGIN
    CREATE INDEX IX_ErrorLogs_IsResolved ON dbo.ErrorLogs (IsResolved);
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = 'IX_ErrorLogs_ErrorTime' AND object_id = OBJECT_ID('dbo.ErrorLogs')
)
BEGIN
    CREATE INDEX IX_ErrorLogs_ErrorTime ON dbo.ErrorLogs (ErrorTime);
END
