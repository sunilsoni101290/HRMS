-- Adds the agent-authentication secret column to BiometricDevices.
-- Run this once against the ERP database, OR let EF Core generate the
-- equivalent migration by running (from the Infrastructure project folder):
--   dotnet ef migrations add AddDeviceKeyToBiometricDevice --startup-project ..\API
--   dotnet ef database update --startup-project ..\API
-- If you use the migration route instead, skip this script.

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.BiometricDevices')
      AND name = 'DeviceKey'
)
BEGIN
    ALTER TABLE dbo.BiometricDevices
    ADD DeviceKey NVARCHAR(100) NULL;
END
GO

-- Backfill a random key for any devices created before this column existed,
-- so existing devices can still be paired with a BiometricAgent.
UPDATE dbo.BiometricDevices
SET DeviceKey = REPLACE(CONVERT(NVARCHAR(36), NEWID()), '-', '')
WHERE DeviceKey IS NULL;
GO
