-- Adds BiometricAttendanceLogs.VerifyMode (nullable) - how the punch was
-- captured on the device: "Fingerprint", "Face", "Card", "Password", "Other".
-- Informational/audit only - safe to add without touching existing rows.
--
-- Idempotent: safe to run more than once.
--
-- NOTE: this repo has EF Core migrations under Infrastructure/Migrations as
-- well (e.g. 20260817153104_NewPendingMigrationChangesBiometricChanges).
-- Pick ONE path - either run this script by hand, OR add an EF migration
-- (`dotnet ef migrations add AddBiometricAttendanceLogVerifyMode`) - not both,
-- or the model snapshot and the real schema will drift apart.

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.BiometricAttendanceLogs')
      AND name = 'VerifyMode'
)
BEGIN
    ALTER TABLE dbo.BiometricAttendanceLogs
        ADD VerifyMode NVARCHAR(30) NULL;
END
GO
