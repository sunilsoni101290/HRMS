-- Adds the Leave Policy Engine (foundational phase) columns to LeaveTypes:
--   MinServiceDaysRequired, AccrualFrequency, AccrualDaysPerCycle,
--   IsEncashable, MaxEncashableDays, ApplicableGender, IsRestrictedHolidayType
-- All new columns are additive with backward-compatible defaults, so every
-- existing LeaveType row keeps its current (fully manual, no accrual)
-- behavior after this script runs.
--
-- Alternative (if you prefer EF migrations):
--   dotnet ef migrations add AddLeavePolicyEngineColumnsToLeaveTypes --project Infrastructure --startup-project API
--   dotnet ef database update --project Infrastructure --startup-project API

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.LeaveTypes')
      AND name = 'MinServiceDaysRequired'
)
BEGIN
    ALTER TABLE dbo.LeaveTypes
    ADD MinServiceDaysRequired INT NOT NULL CONSTRAINT DF_LeaveTypes_MinServiceDaysRequired DEFAULT (0);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.LeaveTypes')
      AND name = 'AccrualFrequency'
)
BEGIN
    -- LeaveAccrualFrequency enum: None = 1, Monthly = 2, Quarterly = 3, Yearly = 4.
    -- Default None (1) preserves today's fully-manual balance behavior for
    -- every existing row.
    ALTER TABLE dbo.LeaveTypes
    ADD AccrualFrequency INT NOT NULL CONSTRAINT DF_LeaveTypes_AccrualFrequency DEFAULT (1);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.LeaveTypes')
      AND name = 'AccrualDaysPerCycle'
)
BEGIN
    ALTER TABLE dbo.LeaveTypes
    ADD AccrualDaysPerCycle DECIMAL(18,2) NOT NULL CONSTRAINT DF_LeaveTypes_AccrualDaysPerCycle DEFAULT (0);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.LeaveTypes')
      AND name = 'IsEncashable'
)
BEGIN
    ALTER TABLE dbo.LeaveTypes
    ADD IsEncashable BIT NOT NULL CONSTRAINT DF_LeaveTypes_IsEncashable DEFAULT (0);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.LeaveTypes')
      AND name = 'MaxEncashableDays'
)
BEGIN
    ALTER TABLE dbo.LeaveTypes
    ADD MaxEncashableDays INT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.LeaveTypes')
      AND name = 'ApplicableGender'
)
BEGIN
    -- LeaveApplicableGender enum: All = 1, Male = 2, Female = 3.
    ALTER TABLE dbo.LeaveTypes
    ADD ApplicableGender INT NOT NULL CONSTRAINT DF_LeaveTypes_ApplicableGender DEFAULT (1);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.LeaveTypes')
      AND name = 'IsRestrictedHolidayType'
)
BEGIN
    ALTER TABLE dbo.LeaveTypes
    ADD IsRestrictedHolidayType BIT NOT NULL CONSTRAINT DF_LeaveTypes_IsRestrictedHolidayType DEFAULT (0);
END
GO
