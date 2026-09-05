-- Employee Work Management module - full removal.
--
-- FULL, IRREVERSIBLE REMOVAL of the Employee Work Management module: Job/
-- Work Assignment, Daily Work Entry, Work Tracking Masters (Clients/Jobs/
-- Job Types/Job Items/Work Activities/Work Entry Reasons/Document Statuses)
-- and its four Work Tracking Reports, per explicit request. This drops the
-- underlying HRMS/ERP tables and removes the menu/permission rows for the
-- module - there is no soft-delete/undo path once this runs; restore from a
-- database backup or git history if this module is ever needed again.
--
-- Idempotent - safe to re-run (every DROP/DELETE is guarded). Run this
-- AFTER deploying the corresponding code removal (controllers/views/
-- services/entities/DbContext/DbSeeder changes) so nothing in the running
-- application still expects these tables to exist.
--
-- Tables dropped in FK-dependency order (children before parents) - see
-- "add Daily Work Entry module tables.sql" / "add Employee Work Assignment
-- module tables.sql" (repo root) for the original CREATE TABLE/FK
-- definitions being reversed here.

-- =====================================================================
-- 1) Transaction tables (reference the masters/assignment tables below)
-- =====================================================================
DROP TABLE IF EXISTS [dbo].[DailyWorkEntries];
DROP TABLE IF EXISTS [dbo].[DailyWorkLogApprovalHistories];
DROP TABLE IF EXISTS [dbo].[EmployeeWorkAssignmentHistories];
DROP TABLE IF EXISTS [dbo].[EmployeeWorkAssignments];
DROP TABLE IF EXISTS [dbo].[DailyWorkLogs];

-- =====================================================================
-- 2) Master/reference tables (in child-before-parent order)
-- =====================================================================
DROP TABLE IF EXISTS [dbo].[JobItems];
DROP TABLE IF EXISTS [dbo].[WorkActivities];
DROP TABLE IF EXISTS [dbo].[WorkJobs];
DROP TABLE IF EXISTS [dbo].[JobTypes];
DROP TABLE IF EXISTS [dbo].[WorkEntryReasons];
DROP TABLE IF EXISTS [dbo].[DocumentStatuses];
DROP TABLE IF EXISTS [dbo].[Clients];
GO

-- =====================================================================
-- 3) Menu/permission cleanup - the 11 AppFeature codes this module seeded
--    (see Domain/Helper/AppFeatureConstants.cs's former "Employee Work
--    Management" block, now removed from the codebase). AppFeatureSeeder.
--    ReconcileModulesAsync only ever adds/updates by Code - it never
--    deletes a feature whose Def() call was removed from code, so these
--    rows must be cleaned up here explicitly or the menu entries would
--    keep showing (pointing at controllers/actions that no longer exist).
-- =====================================================================
DECLARE @Codes TABLE (Code NVARCHAR(100) PRIMARY KEY);
INSERT INTO @Codes (Code) VALUES
    ('EMPLOYEE_WORK_MANAGEMENT'),
    ('DAILY_WORK_ENTRY'),
    ('MY_WORK_ENTRIES'),
    ('WORK_ENTRY_APPROVAL'),
    ('WORK_ASSIGNMENT'),
    ('MY_ASSIGNED_JOBS'),
    ('WORK_TRACKING_MASTERS'),
    ('EMPLOYEE_WORK_REPORT'),
    ('JOB_WISE_WORK_REPORT'),
    ('STRUCTURE_WORK_REPORT'),
    ('WORK_UTILIZATION_REPORT');

-- RolePermissions first (real FK to Permissions.Id - see
-- ApplicationDbContext's RolePermission -> Permission configuration).
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RolePermissions')
   AND EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Permissions')
BEGIN
    DELETE rp
    FROM [dbo].[RolePermissions] rp
    INNER JOIN [dbo].[Permissions] p ON p.Id = rp.PermissionId
    WHERE p.FeatureId IN (SELECT Code FROM @Codes);
END

-- Permissions (Permission.FeatureId is a plain string column holding the
-- owning AppFeature's Code - not a database-enforced FK).
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Permissions')
BEGIN
    DELETE FROM [dbo].[Permissions]
    WHERE FeatureId IN (SELECT Code FROM @Codes);
END

-- AppFeatures themselves (the menu entries).
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AppFeatures')
BEGIN
    DELETE FROM [dbo].[AppFeatures]
    WHERE Code IN (SELECT Code FROM @Codes);
END
GO
