-- Security / User (Index) - "Password Changed" column.
-- Adds Users.PasswordChangedOn, stamped whenever PasswordHash is set
-- (self-registration, self-service change, forgot-password reset, admin
-- create, admin reset - see Domain/Entities/User.cs for the column's XML
-- doc comment and the corresponding code changes in AuthService,
-- EmployeeService and UserService). NULL for any existing user row until
-- that user's password is next changed - there is no reliable historical
-- value to backfill.
--
-- Idempotent - safe to re-run. Mirrors this repo's existing
-- "add DeviceLogs monthly table support columns.sql" style (IF NOT
-- EXISTS-guarded ALTER TABLE). If you have the .NET SDK available,
-- `dotnet ef migrations add AddUserPasswordChangedOn --project Infrastructure
-- --startup-project API` should produce an equivalent migration from the
-- entity change already made to Domain/Entities/User.cs - this script is
-- the ready-to-run equivalent for environments without the SDK on hand
-- (see Infrastructure/Migrations/20260904000000_AddUserPasswordChangedOn.cs
-- for the migration-history-tracked copy of this same statement).

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'PasswordChangedOn')
    ALTER TABLE [dbo].[Users] ADD [PasswordChangedOn] DATETIME2 NULL;
