-- Adds the Remarks column employees use when updating their task status
-- (e.g. "waiting on approval from finance"). Run this once against the ERP
-- database, OR let EF Core generate the equivalent migration by running
-- (from the Infrastructure project folder):
--   dotnet ef migrations add AddRemarksToEmployeeTask --startup-project ..\API
--   dotnet ef database update --startup-project ..\API
-- If you use the migration route instead, skip this script.

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.EmployeeTasks')
      AND name = 'Remarks'
)
BEGIN
    ALTER TABLE dbo.EmployeeTasks
    ADD Remarks NVARCHAR(500) NULL;
END
GO
