IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [ApiRequestLogs] (
    [Id] nvarchar(450) NOT NULL,
    [CorrelationId] nvarchar(max) NOT NULL,
    [Endpoint] nvarchar(max) NOT NULL,
    [Controller] nvarchar(max) NOT NULL,
    [Action] nvarchar(max) NOT NULL,
    [Method] nvarchar(max) NOT NULL,
    [RequestBody] nvarchar(max) NOT NULL,
    [QueryParams] nvarchar(max) NOT NULL,
    [Headers] nvarchar(max) NOT NULL,
    [ResponseBody] nvarchar(max) NOT NULL,
    [StatusCode] int NULL,
    [ExecutionTimeMs] bigint NULL,
    [UserId] nvarchar(max) NOT NULL,
    [UserName] nvarchar(max) NOT NULL,
    [CompanyId] nvarchar(max) NOT NULL,
    [BranchId] nvarchar(max) NULL,
    [IPAddress] nvarchar(max) NOT NULL,
    [UserAgent] nvarchar(max) NOT NULL,
    [Device] nvarchar(max) NOT NULL,
    [IsSuccess] bit NOT NULL,
    [ErrorMessage] nvarchar(max) NOT NULL,
    [ExceptionStackTrace] nvarchar(max) NOT NULL,
    [IsAuthenticated] bit NOT NULL,
    [Token] nvarchar(max) NOT NULL,
    [RequestTime] datetime2 NOT NULL,
    [ResponseTime] datetime2 NULL,
    [Remarks] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_ApiRequestLogs] PRIMARY KEY ([Id])
);

CREATE TABLE [ApiResponseLogs] (
    [Id] nvarchar(50) NOT NULL,
    [RequestId] nvarchar(50) NOT NULL,
    [CorrelationId] nvarchar(100) NOT NULL,
    [Endpoint] nvarchar(500) NOT NULL,
    [Controller] nvarchar(100) NOT NULL,
    [Action] nvarchar(100) NOT NULL,
    [Method] nvarchar(10) NOT NULL,
    [ResponseBody] nvarchar(max) NOT NULL,
    [StatusCode] int NOT NULL,
    [IsSuccess] bit NOT NULL,
    [ExecutionTimeMs] bigint NOT NULL,
    [UserId] nvarchar(50) NOT NULL,
    [UserName] nvarchar(150) NOT NULL,
    [CompanyId] nvarchar(50) NOT NULL,
    [BranchId] nvarchar(50) NULL,
    [IPAddress] nvarchar(50) NOT NULL,
    [UserAgent] nvarchar(500) NOT NULL,
    [ResponseTime] datetime2 NOT NULL,
    [Remarks] nvarchar(500) NOT NULL,
    CONSTRAINT [PK_ApiResponseLogs] PRIMARY KEY ([Id])
);

CREATE TABLE [AppFeatures] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [Code] nvarchar(100) NOT NULL,
    [Module] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NULL,
    [ParentFeatureId] nvarchar(450) NULL,
    [DisplayOrder] int NOT NULL,
    [ControllerName] nvarchar(100) NOT NULL,
    [ActionName] nvarchar(100) NOT NULL,
    [AreaName] nvarchar(100) NULL,
    [IsVisible] bit NOT NULL,
    [IsMenu] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [Icon] nvarchar(100) NULL,
    [BadgeText] nvarchar(50) NULL,
    [CanView] bit NOT NULL,
    [CanAdd] bit NOT NULL,
    [CanEdit] bit NOT NULL,
    [CanDelete] bit NOT NULL,
    [CanApprove] bit NOT NULL,
    [CanExport] bit NOT NULL,
    [CanPrint] bit NOT NULL,
    [AppFeatureType] int NULL,
    [IsHRMSFeature] bit NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_AppFeatures] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AppFeatures_AppFeatures_ParentFeatureId] FOREIGN KEY ([ParentFeatureId]) REFERENCES [AppFeatures] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [AssetCategories] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_AssetCategories] PRIMARY KEY ([Id])
);

CREATE TABLE [BiometricAttendanceLogs] (
    [Id] nvarchar(450) NOT NULL,
    [EmployeeCode] nvarchar(450) NOT NULL,
    [PunchTime] datetime2 NOT NULL,
    [PunchType] int NOT NULL,
    [DeviceId] nvarchar(450) NOT NULL,
    [DeviceTransactionId] nvarchar(100) NULL,
    [VerifyMode] nvarchar(30) NULL,
    [SourceTable] nvarchar(60) NULL,
    [DownloadDate] datetime2 NULL,
    [IsDuplicate] bit NOT NULL,
    [IsProcessed] bit NOT NULL,
    [ProcessedOn] datetime2 NULL,
    [TenantId] nvarchar(450) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_BiometricAttendanceLogs] PRIMARY KEY ([Id])
);

CREATE TABLE [Candidates] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [Email] nvarchar(150) NOT NULL,
    [Phone] nvarchar(15) NOT NULL,
    [TotalExperience] int NULL,
    [Skills] nvarchar(max) NOT NULL,
    [CurrentCompany] nvarchar(max) NOT NULL,
    [CurrentSalary] decimal(18,2) NULL,
    [ExpectedSalary] decimal(18,2) NULL,
    [ResumeUrl] nvarchar(max) NOT NULL,
    [Status] int NOT NULL,
    [Source] nvarchar(max) NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Candidates] PRIMARY KEY ([Id])
);

CREATE TABLE [Countries] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Code] nvarchar(max) NOT NULL,
    [PhoneCode] nvarchar(max) NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Countries] PRIMARY KEY ([Id])
);

CREATE TABLE [ErrorLogs] (
    [Id] nvarchar(50) NOT NULL,
    [RequestId] nvarchar(50) NOT NULL,
    [CorrelationId] nvarchar(100) NOT NULL,
    [ErrorMessage] nvarchar(max) NOT NULL,
    [ExceptionType] nvarchar(200) NOT NULL,
    [StackTrace] nvarchar(max) NOT NULL,
    [InnerException] nvarchar(max) NOT NULL,
    [Endpoint] nvarchar(500) NOT NULL,
    [Controller] nvarchar(100) NOT NULL,
    [Action] nvarchar(100) NOT NULL,
    [Method] nvarchar(10) NOT NULL,
    [ModuleName] nvarchar(150) NULL,
    [FeatureName] nvarchar(150) NULL,
    [RequestBody] nvarchar(max) NOT NULL,
    [QueryParams] nvarchar(max) NOT NULL,
    [UserId] nvarchar(50) NOT NULL,
    [UserName] nvarchar(150) NOT NULL,
    [CompanyId] nvarchar(50) NULL,
    [IPAddress] nvarchar(50) NOT NULL,
    [UserAgent] nvarchar(500) NOT NULL,
    [LogLevel] nvarchar(20) NOT NULL,
    [ErrorTime] datetime2 NOT NULL,
    [IsResolved] bit NOT NULL,
    [ResolvedOn] datetime2 NULL,
    [ResolvedBy] nvarchar(100) NOT NULL,
    [Remarks] nvarchar(500) NOT NULL,
    CONSTRAINT [PK_ErrorLogs] PRIMARY KEY ([Id])
);

CREATE TABLE [EsslAttendanceSyncStates] (
    [Id] nvarchar(450) NOT NULL,
    [LastProcessedDeviceLogId] int NULL,
    [LastProcessedLogDate] datetime2 NULL,
    [LastProcessedSourceTable] nvarchar(60) NULL,
    [LastSyncStartedAt] datetime2 NULL,
    [LastSyncCompletedAt] datetime2 NULL,
    [LastSyncStatus] nvarchar(20) NULL,
    [LastError] nvarchar(1000) NULL,
    [RecordsRead] int NOT NULL,
    [RecordsImported] int NOT NULL,
    [RecordsSkipped] int NOT NULL,
    [RecordsFailed] int NOT NULL,
    [IsSyncRunning] bit NOT NULL,
    [TenantId] nvarchar(450) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_EsslAttendanceSyncStates] PRIMARY KEY ([Id])
);

CREATE TABLE [EsslIntegrationSettings] (
    [Id] nvarchar(450) NOT NULL,
    [IntegrationEnabled] bit NOT NULL,
    [DatabaseServer] nvarchar(300) NOT NULL,
    [DatabaseName] nvarchar(200) NOT NULL,
    [AuthenticationType] nvarchar(20) NOT NULL,
    [Username] nvarchar(200) NULL,
    [EncryptedPassword] nvarchar(max) NULL,
    [ConnectionTimeout] int NOT NULL,
    [SyncIntervalMinutes] int NOT NULL,
    [BatchSize] int NOT NULL,
    [TenantId] nvarchar(450) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_EsslIntegrationSettings] PRIMARY KEY ([Id])
);

CREATE TABLE [FaqItems] (
    [Id] nvarchar(450) NOT NULL,
    [CompanyId] nvarchar(max) NULL,
    [Category] nvarchar(100) NOT NULL,
    [Question] nvarchar(300) NOT NULL,
    [Answer] nvarchar(max) NOT NULL,
    [DisplayOrder] int NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_FaqItems] PRIMARY KEY ([Id])
);

CREATE TABLE [HolidayGroups] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [Description] nvarchar(500) NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_HolidayGroups] PRIMARY KEY ([Id])
);

CREATE TABLE [LeaveTypes] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [MaxDaysPerYear] int NOT NULL,
    [IsPaid] bit NOT NULL,
    [AllowCarryForward] bit NOT NULL,
    [MaxCarryForwardDays] int NULL,
    [AllowHalfDay] bit NOT NULL,
    [MinServiceDaysRequired] int NOT NULL,
    [AccrualFrequency] int NOT NULL,
    [AccrualDaysPerCycle] decimal(18,2) NOT NULL,
    [IsEncashable] bit NOT NULL,
    [MaxEncashableDays] int NULL,
    [ApplicableGender] int NOT NULL,
    [IsRestrictedHolidayType] bit NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LeaveTypes] PRIMARY KEY ([Id])
);

CREATE TABLE [NotificationGroups] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [TenantId] nvarchar(max) NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_NotificationGroups] PRIMARY KEY ([Id])
);

CREATE TABLE [Notifications] (
    [Id] nvarchar(450) NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Message] nvarchar(1000) NOT NULL,
    [FeatureId] nvarchar(max) NULL,
    [ReferenceId] nvarchar(450) NULL,
    [NotificationType] nvarchar(max) NOT NULL,
    [NotificationModule] nvarchar(max) NOT NULL,
    [RedirectUrl] nvarchar(max) NULL,
    [IsBroadcast] bit NOT NULL,
    [Priority] int NOT NULL,
    [ExpiryDate] datetime2 NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id])
);

CREATE TABLE [OnboardingChecklistTemplateItems] (
    [Id] nvarchar(450) NOT NULL,
    [StageType] int NOT NULL,
    [Title] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsMandatory] bit NOT NULL,
    [SortOrder] int NOT NULL,
    [TenantId] nvarchar(450) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_OnboardingChecklistTemplateItems] PRIMARY KEY ([Id])
);

CREATE TABLE [Permissions] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [Code] nvarchar(100) NOT NULL,
    [Module] nvarchar(max) NOT NULL,
    [FeatureId] nvarchar(max) NOT NULL,
    [Action] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [DisplayOrder] int NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id])
);

CREATE TABLE [Roles] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Code] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [TenantId] nvarchar(max) NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
);

CREATE TABLE [SalaryComponents] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [ComponentType] int NOT NULL,
    [IsTaxable] bit NOT NULL,
    [IsPFApplicable] bit NOT NULL,
    [IsESICApplicable] bit NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_SalaryComponents] PRIMARY KEY ([Id])
);

CREATE TABLE [SalaryTemplates] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [Description] nvarchar(500) NULL,
    [EffectiveFrom] datetime2 NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_SalaryTemplates] PRIMARY KEY ([Id])
);

CREATE TABLE [SequenceMasters] (
    [Id] nvarchar(450) NOT NULL,
    [Prefix] nvarchar(max) NOT NULL,
    [FinancialYearId] nvarchar(max) NOT NULL,
    [CurrentNumber] int NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_SequenceMasters] PRIMARY KEY ([Id])
);

CREATE TABLE [Subscriptions] (
    [Id] nvarchar(450) NOT NULL,
    [TenantId] nvarchar(max) NOT NULL,
    [TenantTypeId] nvarchar(max) NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [AmountPaid] decimal(18,2) NOT NULL,
    [PaymentStatus] int NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Subscriptions] PRIMARY KEY ([Id])
);

CREATE TABLE [WeekOffs] (
    [Id] nvarchar(450) NOT NULL,
    [Day] int NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_WeekOffs] PRIMARY KEY ([Id])
);

CREATE TABLE [Assets] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [AssetCode] nvarchar(100) NOT NULL,
    [SerialNumber] nvarchar(100) NOT NULL,
    [AssetCategoryId] nvarchar(450) NOT NULL,
    [PurchaseDate] datetime2 NULL,
    [PurchaseCost] decimal(18,2) NULL,
    [VendorName] nvarchar(max) NOT NULL,
    [WarrantyExpiryDate] datetime2 NULL,
    [Status] nvarchar(max) NOT NULL,
    [CompanyId] nvarchar(max) NOT NULL,
    [BranchId] nvarchar(max) NULL,
    [Description] nvarchar(max) NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Assets] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Assets_AssetCategories_AssetCategoryId] FOREIGN KEY ([AssetCategoryId]) REFERENCES [AssetCategories] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [States] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Code] nvarchar(max) NOT NULL,
    [CountryId] nvarchar(450) NOT NULL,
    [GSTStateCode] nvarchar(max) NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_States] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_States_Countries_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [Countries] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [HolidayGroupDetails] (
    [Id] nvarchar(450) NOT NULL,
    [HolidayGroupId] nvarchar(450) NOT NULL,
    [HolidayDate] datetime2 NOT NULL,
    [HolidayName] nvarchar(200) NOT NULL,
    [Remarks] nvarchar(500) NULL,
    [IsOptional] bit NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_HolidayGroupDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_HolidayGroupDetails_HolidayGroups_HolidayGroupId] FOREIGN KEY ([HolidayGroupId]) REFERENCES [HolidayGroups] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [NotificationGroupAccesses] (
    [Id] nvarchar(450) NOT NULL,
    [NotificationGroupId] nvarchar(450) NOT NULL,
    [AppFeatureId] nvarchar(max) NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_NotificationGroupAccesses] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_NotificationGroupAccesses_NotificationGroups_NotificationGroupId] FOREIGN KEY ([NotificationGroupId]) REFERENCES [NotificationGroups] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [RoleFeatures] (
    [Id] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    [AppFeatureId] nvarchar(450) NOT NULL,
    [IsEnabled] bit NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_RoleFeatures] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RoleFeatures_AppFeatures_AppFeatureId] FOREIGN KEY ([AppFeatureId]) REFERENCES [AppFeatures] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_RoleFeatures_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [RolePermissions] (
    [Id] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    [PermissionId] nvarchar(450) NOT NULL,
    [IsAllowed] bit NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RolePermissions_Permissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [Permissions] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_RolePermissions_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [SalaryTemplateDetails] (
    [Id] nvarchar(450) NOT NULL,
    [SalaryTemplateId] nvarchar(450) NOT NULL,
    [SalaryComponentId] nvarchar(450) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [CalculationType] int NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_SalaryTemplateDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SalaryTemplateDetails_SalaryComponents_SalaryComponentId] FOREIGN KEY ([SalaryComponentId]) REFERENCES [SalaryComponents] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SalaryTemplateDetails_SalaryTemplates_SalaryTemplateId] FOREIGN KEY ([SalaryTemplateId]) REFERENCES [SalaryTemplates] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [AssetHistories] (
    [Id] nvarchar(450) NOT NULL,
    [AssetId] nvarchar(450) NOT NULL,
    [Action] nvarchar(max) NOT NULL,
    [ReferenceId] nvarchar(max) NULL,
    [ActionDate] datetime2 NOT NULL,
    [PerformedBy] nvarchar(max) NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_AssetHistories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AssetHistories_Assets_AssetId] FOREIGN KEY ([AssetId]) REFERENCES [Assets] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Cities] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [StateId] nvarchar(450) NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Cities] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Cities_States_StateId] FOREIGN KEY ([StateId]) REFERENCES [States] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Tenants] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Code] nvarchar(100) NOT NULL,
    [Domain] nvarchar(200) NULL,
    [SubDomain] nvarchar(200) NULL,
    [Email] nvarchar(150) NULL,
    [Phone] nvarchar(15) NOT NULL,
    [Address] nvarchar(300) NOT NULL,
    [Pincode] nvarchar(max) NOT NULL,
    [CountryId] nvarchar(450) NOT NULL,
    [StateId] nvarchar(450) NOT NULL,
    [CityId] nvarchar(450) NOT NULL,
    [SubscriptionStartDate] datetime2 NULL,
    [SubscriptionEndDate] datetime2 NULL,
    [Logo] nvarchar(max) NULL,
    [WebsiteUrl] nvarchar(250) NULL,
    [ConnectionString] nvarchar(max) NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Tenants] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Tenants_Cities_CityId] FOREIGN KEY ([CityId]) REFERENCES [Cities] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Tenants_Countries_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [Countries] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Tenants_States_StateId] FOREIGN KEY ([StateId]) REFERENCES [States] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [AdvanceTypes] (
    [Id] nvarchar(450) NOT NULL,
    [TenantId] nvarchar(450) NOT NULL,
    [Code] nvarchar(20) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NULL,
    [MaxAmount] decimal(18,2) NULL,
    [MaxAmountSalaryMultiplier] decimal(18,2) NULL,
    [MaxInstallments] int NOT NULL,
    [IsInterestFree] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_AdvanceTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AdvanceTypes_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Companies] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [GSTNumber] nvarchar(15) NULL,
    [PANNumber] nvarchar(10) NULL,
    [CINNumber] nvarchar(50) NULL,
    [Email] nvarchar(150) NULL,
    [Phone] nvarchar(15) NOT NULL,
    [AlternatePhone] nvarchar(15) NULL,
    [Address] nvarchar(500) NULL,
    [Pincode] nvarchar(10) NOT NULL,
    [OwnershipType] int NOT NULL,
    [BusinessCategory] int NOT NULL,
    [TenantId] nvarchar(450) NOT NULL,
    [CountryId] nvarchar(450) NOT NULL,
    [StateId] nvarchar(450) NOT NULL,
    [CityId] nvarchar(450) NOT NULL,
    [IncorporationDate] datetime2 NULL,
    [Logo] nvarchar(max) NULL,
    [WebsiteUrl] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Companies] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Companies_Cities_CityId] FOREIGN KEY ([CityId]) REFERENCES [Cities] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Companies_Countries_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [Countries] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Companies_States_StateId] FOREIGN KEY ([StateId]) REFERENCES [States] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Companies_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [LoanTypes] (
    [Id] nvarchar(450) NOT NULL,
    [TenantId] nvarchar(450) NOT NULL,
    [Code] nvarchar(20) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NULL,
    [InterestMethod] int NOT NULL,
    [DefaultInterestRatePercent] decimal(18,2) NOT NULL,
    [MaxTenureMonths] int NOT NULL,
    [RequiresGuarantor] bit NOT NULL,
    [RequiresCollateral] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LoanTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LoanTypes_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Shifts] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [StartTime] time NOT NULL,
    [EndTime] time NOT NULL,
    [GraceInMinutes] int NOT NULL,
    [GraceOutMinutes] int NOT NULL,
    [HalfDayMinutes] int NOT NULL,
    [FullDayMinutes] int NOT NULL,
    [IsNightShift] bit NOT NULL,
    [IsDefaultShift] bit NOT NULL,
    [MinimumWorkingMinutes] int NOT NULL,
    [MaximumWorkingMinutes] int NOT NULL,
    [TenantId] nvarchar(450) NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Shifts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Shifts_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [TenantFeatures] (
    [Id] nvarchar(450) NOT NULL,
    [TenantId] nvarchar(450) NOT NULL,
    [AppFeatureId] nvarchar(450) NOT NULL,
    [IsEnabled] bit NOT NULL,
    [ExpiryDate] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_TenantFeatures] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TenantFeatures_AppFeatures_AppFeatureId] FOREIGN KEY ([AppFeatureId]) REFERENCES [AppFeatures] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TenantFeatures_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [AttendancePolicies] (
    [Id] nvarchar(450) NOT NULL,
    [CompanyId] nvarchar(450) NULL,
    [PolicyName] nvarchar(200) NOT NULL,
    [EffectiveFrom] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    [MaxRegularizationRequestsPerMonth] int NOT NULL,
    [MaxWfhDaysPerMonth] int NOT NULL,
    [LateMarkGraceCount] int NOT NULL,
    [LateMarkPenaltyType] int NOT NULL,
    [MinimumAttendancePercentForFullSalary] decimal(18,2) NOT NULL,
    [CompOffEligibleExtraHours] decimal(18,2) NOT NULL,
    [ShortLeaveHoursPerDay] decimal(18,2) NOT NULL,
    [SalaryProrationBasis] int NOT NULL,
    [FixedWorkingDaysPerMonth] decimal(18,2) NOT NULL,
    [Remarks] nvarchar(500) NULL,
    [TenantId] nvarchar(450) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_AttendancePolicies] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AttendancePolicies_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Branches] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [CompanyId] nvarchar(450) NOT NULL,
    [TenantId] nvarchar(450) NOT NULL,
    [Email] nvarchar(150) NULL,
    [Phone] nvarchar(15) NOT NULL,
    [AlternatePhone] nvarchar(15) NULL,
    [Address] nvarchar(300) NOT NULL,
    [Pincode] nvarchar(10) NOT NULL,
    [CountryId] nvarchar(450) NOT NULL,
    [StateId] nvarchar(450) NOT NULL,
    [CityId] nvarchar(450) NOT NULL,
    [GSTNumber] nvarchar(15) NULL,
    [CINNo] nvarchar(max) NULL,
    [IsHeadOffice] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Branches] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Branches_Cities_CityId] FOREIGN KEY ([CityId]) REFERENCES [Cities] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Branches_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Branches_Countries_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [Countries] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Branches_States_StateId] FOREIGN KEY ([StateId]) REFERENCES [States] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Branches_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [FinancialYears] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Code] nvarchar(max) NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [Status] int NOT NULL,
    [TenantId] nvarchar(450) NOT NULL,
    [CompanyId] nvarchar(450) NOT NULL,
    [IsCurrent] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_FinancialYears] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FinancialYears_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_FinancialYears_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [BiometricAgents] (
    [Id] nvarchar(450) NOT NULL,
    [TenantId] nvarchar(450) NOT NULL,
    [AgentCode] nvarchar(50) NOT NULL,
    [AgentName] nvarchar(100) NOT NULL,
    [AgentKey] nvarchar(100) NULL,
    [BranchId] nvarchar(450) NULL,
    [Description] nvarchar(500) NULL,
    [MachineName] nvarchar(max) NULL,
    [AgentVersion] nvarchar(max) NULL,
    [LastHeartbeat] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_BiometricAgents] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BiometricAgents_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_BiometricAgents_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Departments] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [TenantId] nvarchar(450) NOT NULL,
    [CompanyId] nvarchar(450) NULL,
    [BranchId] nvarchar(450) NULL,
    [ParentDepartmentId] nvarchar(450) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Departments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Departments_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Departments_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Departments_Departments_ParentDepartmentId] FOREIGN KEY ([ParentDepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Departments_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [LoanPolicies] (
    [Id] nvarchar(450) NOT NULL,
    [TenantId] nvarchar(450) NOT NULL,
    [CompanyId] nvarchar(450) NULL,
    [BranchId] nvarchar(450) NULL,
    [LoanTypeId] nvarchar(450) NOT NULL,
    [MinAmount] decimal(18,2) NOT NULL,
    [MaxAmount] decimal(18,2) NOT NULL,
    [MinTenureMonths] int NOT NULL,
    [MaxTenureMonths] int NOT NULL,
    [InterestRatePercent] decimal(18,2) NULL,
    [MinServiceMonthsRequired] int NOT NULL,
    [MaxActiveLoans] int NOT NULL,
    [MaxDeductionPercentOfNetSalary] decimal(18,2) NOT NULL,
    [EligibilitySalaryMultiplier] decimal(18,2) NOT NULL,
    [PreClosurePenaltyPercent] decimal(18,2) NOT NULL,
    [VersionNumber] int NOT NULL,
    [EffectiveFrom] datetime2 NOT NULL,
    [EffectiveTo] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LoanPolicies] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LoanPolicies_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LoanPolicies_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LoanPolicies_LoanTypes_LoanTypeId] FOREIGN KEY ([LoanTypeId]) REFERENCES [LoanTypes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LoanPolicies_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Locations] (
    [Id] nvarchar(450) NOT NULL,
    [LocationName] nvarchar(200) NOT NULL,
    [LocationCode] nvarchar(50) NOT NULL,
    [BranchId] nvarchar(450) NULL,
    [Address] nvarchar(max) NULL,
    [IsDefault] bit NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Locations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Locations_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [TaxSlabs] (
    [Id] nvarchar(450) NOT NULL,
    [FinancialYearId] nvarchar(450) NOT NULL,
    [Regime] int NOT NULL,
    [SlabOrder] int NOT NULL,
    [MinIncome] decimal(18,2) NOT NULL,
    [MaxIncome] decimal(18,2) NULL,
    [RatePercent] decimal(18,2) NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_TaxSlabs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TaxSlabs_FinancialYears_FinancialYearId] FOREIGN KEY ([FinancialYearId]) REFERENCES [FinancialYears] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [BiometricDevices] (
    [Id] nvarchar(450) NOT NULL,
    [TenantId] nvarchar(450) NOT NULL,
    [CompanyId] nvarchar(450) NOT NULL,
    [BranchId] nvarchar(450) NULL,
    [DeviceName] nvarchar(100) NOT NULL,
    [DeviceCode] nvarchar(50) NOT NULL,
    [IPAddress] nvarchar(max) NOT NULL,
    [Port] int NOT NULL,
    [ApiUrl] nvarchar(max) NULL,
    [Username] nvarchar(max) NULL,
    [Password] nvarchar(max) NULL,
    [SerialNumber] nvarchar(max) NULL,
    [LastSyncDate] datetime2 NULL,
    [LastSeen] datetime2 NULL,
    [DeviceKey] nvarchar(100) NULL,
    [DeviceType] nvarchar(30) NOT NULL,
    [CommunicationType] nvarchar(30) NOT NULL,
    [CommKey] int NOT NULL,
    [AgentId] nvarchar(450) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_BiometricDevices] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BiometricDevices_BiometricAgents_AgentId] FOREIGN KEY ([AgentId]) REFERENCES [BiometricAgents] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_BiometricDevices_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_BiometricDevices_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_BiometricDevices_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Announcements] (
    [Id] nvarchar(450) NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Message] nvarchar(1000) NOT NULL,
    [AnnouncementType] int NOT NULL,
    [PublishDate] datetime2 NOT NULL,
    [ExpiryDate] datetime2 NULL,
    [IsForAll] bit NOT NULL,
    [DepartmentId] nvarchar(450) NULL,
    [RoleId] nvarchar(450) NULL,
    [AttachmentUrl] nvarchar(max) NULL,
    [Priority] int NOT NULL,
    [CompanyId] nvarchar(max) NOT NULL,
    [BranchId] nvarchar(max) NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Announcements] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Announcements_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Announcements_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Designations] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [TenantId] nvarchar(450) NOT NULL,
    [CompanyId] nvarchar(450) NULL,
    [BranchId] nvarchar(450) NULL,
    [DepartmentId] nvarchar(450) NOT NULL,
    [ParentDesignationId] nvarchar(450) NULL,
    [Level] int NOT NULL,
    [MinSalary] decimal(18,2) NOT NULL,
    [MaxSalary] decimal(18,2) NOT NULL,
    [ProbationPeriodMonths] int NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Designations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Designations_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Designations_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Designations_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Designations_Designations_ParentDesignationId] FOREIGN KEY ([ParentDesignationId]) REFERENCES [Designations] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Designations_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Events] (
    [Id] nvarchar(450) NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [StartTime] time NULL,
    [EndTime] time NULL,
    [Location] nvarchar(200) NOT NULL,
    [EventType] int NOT NULL,
    [OrganizedBy] nvarchar(max) NOT NULL,
    [IsForAll] bit NOT NULL,
    [DepartmentId] nvarchar(450) NULL,
    [RoleId] nvarchar(450) NULL,
    [SendReminder] bit NOT NULL,
    [ReminderBeforeMinutes] int NULL,
    [CompanyId] nvarchar(max) NOT NULL,
    [BranchId] nvarchar(max) NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Events] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Events_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Events_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [BiometricDeviceTestRequests] (
    [Id] nvarchar(450) NOT NULL,
    [TenantId] nvarchar(450) NOT NULL,
    [DeviceId] nvarchar(450) NOT NULL,
    [AgentId] nvarchar(450) NULL,
    [Status] int NOT NULL,
    [Stage] nvarchar(50) NULL,
    [Message] nvarchar(500) NULL,
    [DeviceInfo] nvarchar(200) NULL,
    [RequestedBy] nvarchar(max) NOT NULL,
    [RequestedOn] datetime2 NOT NULL,
    [CompletedOn] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_BiometricDeviceTestRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BiometricDeviceTestRequests_BiometricDevices_DeviceId] FOREIGN KEY ([DeviceId]) REFERENCES [BiometricDevices] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_BiometricDeviceTestRequests_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [BiometricSyncLogs] (
    [Id] nvarchar(450) NOT NULL,
    [TenantId] nvarchar(450) NOT NULL,
    [DeviceId] nvarchar(450) NULL,
    [AgentId] nvarchar(max) NULL,
    [SyncType] nvarchar(30) NOT NULL,
    [StartTime] datetime2 NOT NULL,
    [EndTime] datetime2 NULL,
    [RecordsFetched] int NOT NULL,
    [RecordsInserted] int NOT NULL,
    [RecordsSkipped] int NOT NULL,
    [RecordsFailed] int NOT NULL,
    [DuplicateCount] int NOT NULL,
    [UnmappedCount] int NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [ErrorMessage] nvarchar(500) NULL,
    [FromDate] datetime2 NULL,
    [ToDate] datetime2 NULL,
    [TriggeredBy] nvarchar(100) NULL,
    [SourceTables] nvarchar(500) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_BiometricSyncLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BiometricSyncLogs_BiometricDevices_DeviceId] FOREIGN KEY ([DeviceId]) REFERENCES [BiometricDevices] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_BiometricSyncLogs_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Employees] (
    [Id] nvarchar(450) NOT NULL,
    [EmployeeCode] nvarchar(50) NOT NULL,
    [FirstName] nvarchar(100) NOT NULL,
    [LastName] nvarchar(100) NULL,
    [TenantId] nvarchar(450) NOT NULL,
    [CompanyId] nvarchar(450) NOT NULL,
    [BranchId] nvarchar(450) NULL,
    [DepartmentId] nvarchar(450) NULL,
    [DesignationId] nvarchar(450) NULL,
    [ReportingManagerId] nvarchar(450) NULL,
    [DateOfBirth] datetime2 NULL,
    [Gender] int NOT NULL,
    [MaritalStatus] int NOT NULL,
    [Email] nvarchar(150) NULL,
    [Phone] nvarchar(15) NULL,
    [EmergencyContact] nvarchar(15) NULL,
    [Address] nvarchar(max) NULL,
    [Pincode] nvarchar(max) NULL,
    [PANNumber] nvarchar(10) NULL,
    [AadharNumber] nvarchar(12) NULL,
    [FilePath] nvarchar(max) NULL,
    [ShiftId] nvarchar(450) NOT NULL,
    [JoiningDate] datetime2 NOT NULL,
    [ConfirmationDate] datetime2 NULL,
    [RelievingDate] datetime2 NULL,
    [ProbationEndDate] datetime2 NULL,
    [PassportNumber] nvarchar(max) NULL,
    [IssueDate] datetime2 NULL,
    [ExpiryDate] datetime2 NULL,
    [PlaceOfIssue] nvarchar(max) NULL,
    [Nationality] int NOT NULL,
    [PassportStatus] int NOT NULL,
    [CountryId] nvarchar(450) NULL,
    [PassportFilePath] nvarchar(max) NULL,
    [EmploymentType] int NOT NULL,
    [CandidateId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Employees] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Employees_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Employees_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Employees_Countries_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [Countries] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Employees_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Employees_Designations_DesignationId] FOREIGN KEY ([DesignationId]) REFERENCES [Designations] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Employees_Employees_ReportingManagerId] FOREIGN KEY ([ReportingManagerId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Employees_Shifts_ShiftId] FOREIGN KEY ([ShiftId]) REFERENCES [Shifts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Employees_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [JobOpenings] (
    [Id] nvarchar(450) NOT NULL,
    [Title] nvarchar(150) NOT NULL,
    [DepartmentId] nvarchar(450) NOT NULL,
    [DesignationId] nvarchar(450) NOT NULL,
    [VacancyCount] int NOT NULL,
    [MinSalary] decimal(18,2) NULL,
    [MaxSalary] decimal(18,2) NULL,
    [JobDescription] nvarchar(max) NOT NULL,
    [RequiredSkills] nvarchar(max) NOT NULL,
    [Status] int NOT NULL,
    [PostedDate] datetime2 NOT NULL,
    [ClosingDate] datetime2 NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_JobOpenings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_JobOpenings_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_JobOpenings_Designations_DesignationId] FOREIGN KEY ([DesignationId]) REFERENCES [Designations] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ApprovalDelegations] (
    [Id] nvarchar(450) NOT NULL,
    [DelegatorEmployeeId] nvarchar(450) NOT NULL,
    [DelegateEmployeeId] nvarchar(450) NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [Reason] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_ApprovalDelegations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ApprovalDelegations_Employees_DelegateEmployeeId] FOREIGN KEY ([DelegateEmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ApprovalDelegations_Employees_DelegatorEmployeeId] FOREIGN KEY ([DelegatorEmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [AssetAllocations] (
    [Id] nvarchar(450) NOT NULL,
    [AssetId] nvarchar(450) NOT NULL,
    [EmployeeId] nvarchar(450) NOT NULL,
    [AllocatedOn] datetime2 NOT NULL,
    [ReturnedOn] datetime2 NULL,
    [AllocationStatus] int NOT NULL,
    [ConditionOnIssue] nvarchar(max) NOT NULL,
    [ConditionOnReturn] nvarchar(max) NULL,
    [Remarks] nvarchar(max) NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_AssetAllocations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AssetAllocations_Assets_AssetId] FOREIGN KEY ([AssetId]) REFERENCES [Assets] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AssetAllocations_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Attendances] (
    [Id] nvarchar(450) NOT NULL,
    [TenantId] nvarchar(450) NOT NULL,
    [CompanyId] nvarchar(450) NOT NULL,
    [BranchId] nvarchar(450) NULL,
    [EmployeeId] nvarchar(450) NOT NULL,
    [Date] datetime2 NOT NULL,
    [ShiftId] nvarchar(450) NOT NULL,
    [FirstIn] datetime2 NULL,
    [LastOut] datetime2 NULL,
    [TotalWorkingHours] decimal(18,2) NOT NULL,
    [BreakHours] decimal(18,2) NOT NULL,
    [OvertimeHours] decimal(18,2) NOT NULL,
    [Status] int NOT NULL,
    [IsLate] bit NOT NULL,
    [IsEarlyExit] bit NOT NULL,
    [Remarks] nvarchar(max) NULL,
    [IsManualEntry] bit NOT NULL,
    [IsBiometricAttendance] bit NOT NULL,
    [ProcessedOn] datetime2 NULL,
    [ProcessedBy] nvarchar(max) NULL,
    [SourceDeviceId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Attendances] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Attendances_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Attendances_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Attendances_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Attendances_Shifts_ShiftId] FOREIGN KEY ([ShiftId]) REFERENCES [Shifts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Attendances_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [EmployeeBankDetails] (
    [Id] nvarchar(450) NOT NULL,
    [EmployeeId] nvarchar(450) NOT NULL,
    [BankName] nvarchar(150) NOT NULL,
    [BranchName] nvarchar(150) NOT NULL,
    [AccountNumber] nvarchar(25) NOT NULL,
    [AccountHolderName] nvarchar(150) NOT NULL,
    [IFSCCode] nvarchar(20) NOT NULL,
    [MICRCode] nvarchar(20) NULL,
    [AccountType] int NOT NULL,
    [CancelledChequeFilePath] nvarchar(300) NULL,
    [IsPrimary] bit NOT NULL,
    [Remarks] nvarchar(500) NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_EmployeeBankDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmployeeBankDetails_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [EmployeeBiometricMappings] (
    [Id] nvarchar(450) NOT NULL,
    [EmployeeId] nvarchar(450) NOT NULL,
    [BiometricEmployeeCode] nvarchar(max) NOT NULL,
    [CardNumber] nvarchar(max) NULL,
    [FaceId] nvarchar(max) NULL,
    [FingerTemplateId] nvarchar(max) NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_EmployeeBiometricMappings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmployeeBiometricMappings_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [EmployeeDocuments] (
    [Id] nvarchar(450) NOT NULL,
    [EmployeeId] nvarchar(450) NOT NULL,
    [DocumentType] int NOT NULL,
    [DocumentNumber] nvarchar(100) NULL,
    [DocumentName] nvarchar(200) NULL,
    [FileName] nvarchar(300) NULL,
    [FilePath] nvarchar(500) NOT NULL,
    [FileExtension] nvarchar(20) NULL,
    [FileSize] bigint NULL,
    [IssueDate] datetime2 NULL,
    [ExpiryDate] datetime2 NULL,
    [IssuedBy] nvarchar(200) NULL,
    [IsVerified] bit NOT NULL,
    [VerifiedOn] datetime2 NULL,
    [VerifiedBy] nvarchar(100) NULL,
    [Remarks] nvarchar(500) NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_EmployeeDocuments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmployeeDocuments_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [EmployeeEducationDetails] (
    [Id] nvarchar(450) NOT NULL,
    [EmployeeId] nvarchar(450) NOT NULL,
    [Qualification] nvarchar(100) NOT NULL,
    [Degree] nvarchar(150) NOT NULL,
    [Specialization] nvarchar(150) NOT NULL,
    [InstituteName] nvarchar(200) NOT NULL,
    [University] nvarchar(150) NOT NULL,
    [Board] nvarchar(100) NOT NULL,
    [PassingYear] int NOT NULL,
    [PercentageOrCGPA] decimal(5,2) NOT NULL,
    [Grade] nvarchar(20) NOT NULL,
    [IsHighestQualification] bit NOT NULL,
    [Remarks] nvarchar(500) NOT NULL,
    [CertificateFilePath] nvarchar(300) NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_EmployeeEducationDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmployeeEducationDetails_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [EmployeeESICDetails] (
    [Id] nvarchar(450) NOT NULL,
    [TenantId] nvarchar(max) NOT NULL,
    [EmployeeId] nvarchar(450) NOT NULL,
    [ESICNumber] nvarchar(20) NOT NULL,
    [RegistrationDate] datetime2 NULL,
    [ExitDate] datetime2 NULL,
    [ESICDispensary] nvarchar(150) NOT NULL,
    [BranchOffice] nvarchar(200) NOT NULL,
    [IsEligible] bit NOT NULL,
    [Remarks] nvarchar(500) NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_EmployeeESICDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmployeeESICDetails_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [EmployeeFeedbacks] (
    [Id] nvarchar(450) NOT NULL,
    [EmployeeId] nvarchar(450) NOT NULL,
    [GivenByUserId] nvarchar(max) NOT NULL,
    [FeedbackDate] datetime2 NOT NULL,
    [Category] int NOT NULL,
    [Rating] int NULL,
    [Strengths] nvarchar(max) NULL,
    [AreasOfImprovement] nvarchar(max) NULL,
    [Comments] nvarchar(max) NULL,
    [IsVisibleToEmployee] bit NOT NULL,
    [TenantId] nvarchar(450) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_EmployeeFeedbacks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmployeeFeedbacks_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [EmployeePFDetails] (
    [Id] nvarchar(450) NOT NULL,
    [EmployeeId] nvarchar(450) NOT NULL,
    [UANNumber] nvarchar(20) NOT NULL,
    [PFNumber] nvarchar(50) NOT NULL,
    [PFOffice] nvarchar(150) NOT NULL,
    [PFJoiningDate] datetime2 NULL,
    [PFExitDate] datetime2 NULL,
    [EPSApplicable] bit NOT NULL,
    [EPFApplicable] bit NOT NULL,
    [EDLIApplicable] bit NOT NULL,
    [IsInternationalWorker] bit NOT NULL,
    [Remarks] nvarchar(500) NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_EmployeePFDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmployeePFDetails_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [EmployeeShiftMappings] (
    [Id] nvarchar(450) NOT NULL,
    [EmployeeId] nvarchar(450) NOT NULL,
    [ShiftId] nvarchar(450) NOT NULL,
    [EffectiveFrom] datetime2 NOT NULL,
    [EffectiveTo] datetime2 NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_EmployeeShiftMappings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmployeeShiftMappings_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_EmployeeShiftMappings_Shifts_ShiftId] FOREIGN KEY ([ShiftId]) REFERENCES [Shifts] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [EmployeeTasks] (
    [Id] nvarchar(450) NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(max) NULL,
    [EmployeeId] nvarchar(450) NOT NULL,
    [DueDate] datetime2 NULL,
    [Status] nvarchar(50) NOT NULL,
    [Priority] nvarchar(50) NOT NULL,
    [AssignedBy] nvarchar(max) NULL,
    [CompletedDate] datetime2 NULL,
    [Remarks] nvarchar(500) NULL,
    [CompanyId] nvarchar(max) NOT NULL,
    [BranchId] nvarchar(max) NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_EmployeeTasks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmployeeTasks_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [EmployeeTransfers] (
    [Id] nvarchar(450) NOT NULL,
    [EmployeeId] nvarchar(450) NOT NULL,
    [EffectiveDate] datetime2 NOT NULL,
    [Reason] nvarchar(max) NOT NULL,
    [FromCompanyId] nvarchar(max) NOT NULL,
    [FromBranchId] nvarchar(max) NULL,
    [FromDepartmentId] nvarchar(max) NOT NULL,
    [FromDesignationId] nvarchar(max) NOT NULL,
    [FromReportingManagerId] nvarchar(max) NULL,
    [ToCompanyId] nvarchar(max) NULL,
    [ToBranchId] nvarchar(max) NULL,
    [ToDepartmentId] nvarchar(max) NULL,
    [ToDesignationId] nvarchar(max) NULL,
    [ToReportingManagerId] nvarchar(max) NULL,
    [MakerId] nvarchar(max) NOT NULL,
    [MakerActionOn] datetime2 NOT NULL,
    [MakerRemarks] nvarchar(1000) NULL,
    [Status] int NOT NULL,
    [CheckerId] nvarchar(max) NULL,
    [CheckerActionOn] datetime2 NULL,
    [CheckerRemarks] nvarchar(1000) NULL,
    [TenantId] nvarchar(450) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_EmployeeTransfers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmployeeTransfers_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [EventParticipants] (
    [Id] nvarchar(450) NOT NULL,
    [EventId] nvarchar(450) NOT NULL,
    [EmployeeId] nvarchar(450) NOT NULL,
    [Status] int NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_EventParticipants] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EventParticipants_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_EventParticipants_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [LeaveApplications] (
    [Id] nvarchar(450) NOT NULL,
    [CompanyId] nvarchar(max) NOT NULL,
    [BranchId] nvarchar(max) NULL,
    [EmployeeId] nvarchar(450) NOT NULL,
    [LeaveTypeId] nvarchar(450) NOT NULL,
    [FromDate] datetime2 NOT NULL,
    [ToDate] datetime2 NOT NULL,
    [TotalDays] decimal(18,2) NOT NULL,
    [IsHalfDay] bit NOT NULL,
    [HalfDayType] int NOT NULL,
    [Reason] nvarchar(max) NULL,
    [Status] int NOT NULL,
    [CurrentLevel] int NOT NULL,
    [ApprovedBy] nvarchar(max) NULL,
    [ApprovedDate] datetime2 NULL,
    [RejectedReason] nvarchar(max) NULL,
    [SendBackReason] nvarchar(max) NULL,
    [DocumentUrl] nvarchar(max) NULL,
    [LastReminderSentOn] datetime2 NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LeaveApplications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LeaveApplications_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LeaveApplications_LeaveTypes_LeaveTypeId] FOREIGN KEY ([LeaveTypeId]) REFERENCES [LeaveTypes] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [LeaveBalances] (
    [Id] nvarchar(450) NOT NULL,
    [EmployeeId] nvarchar(450) NOT NULL,
    [LeaveTypeId] nvarchar(450) NOT NULL,
    [Year] int NOT NULL,
    [OpeningBalance] decimal(18,2) NOT NULL,
    [Allocated] decimal(18,2) NOT NULL,
    [Credited] decimal(18,2) NOT NULL,
    [CarryForward] decimal(18,2) NOT NULL,
    [Used] decimal(18,2) NOT NULL,
    [Balance] decimal(18,2) NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LeaveBalances] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LeaveBalances_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LeaveBalances_LeaveTypes_LeaveTypeId] FOREIGN KEY ([LeaveTypeId]) REFERENCES [LeaveTypes] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [LeaveBalanceTransactions] (
    [Id] nvarchar(450) NOT NULL,
    [EmployeeId] nvarchar(450) NOT NULL,
    [LeaveTypeId] nvarchar(450) NOT NULL,
    [Year] int NOT NULL,
    [TransactionType] int NOT NULL,
    [Quantity] decimal(18,2) NOT NULL,
    [BalanceBefore] decimal(18,2) NOT NULL,
    [BalanceAfter] decimal(18,2) NOT NULL,
    [Remarks] nvarchar(max) NULL,
    [TransactionDate] datetime2 NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LeaveBalanceTransactions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LeaveBalanceTransactions_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LeaveBalanceTransactions_LeaveTypes_LeaveTypeId] FOREIGN KEY ([LeaveTypeId]) REFERENCES [LeaveTypes] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [OnboardingCases] (
    [Id] nvarchar(450) NOT NULL,
    [EmployeeId] nvarchar(450) NOT NULL,
    [CandidateId] nvarchar(450) NULL,
    [CompanyId] nvarchar(max) NULL,
    [BranchId] nvarchar(max) NULL,
    [StartDate] datetime2 NOT NULL,
    [TargetCompletionDate] datetime2 NULL,
    [Status] int NOT NULL,
    [CompletedOn] datetime2 NULL,
    [Remarks] nvarchar(max) NULL,
    [TenantId] nvarchar(450) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_OnboardingCases] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OnboardingCases_Candidates_CandidateId] FOREIGN KEY ([CandidateId]) REFERENCES [Candidates] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_OnboardingCases_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [OnDutyRequests] (
    [Id] nvarchar(450) NOT NULL,
    [EmployeeId] nvarchar(450) NOT NULL,
    [FromDate] datetime2 NOT NULL,
    [ToDate] datetime2 NOT NULL,
    [TotalDays] int NOT NULL,
    [Purpose] nvarchar(max) NOT NULL,
    [Location] nvarchar(max) NULL,
    [Status] int NOT NULL,
    [ApprovedBy] nvarchar(max) NULL,
    [ApprovedOn] datetime2 NULL,
    [RejectedBy] nvarchar(max) NULL,
    [RejectedReason] nvarchar(max) NULL,
    [RejectedOn] datetime2 NULL,
    [CancelledOn] datetime2 NULL,
    [CancelledBy] nvarchar(max) NULL,
    [Remarks] nvarchar(500) NULL,
    [TenantId] nvarchar(450) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_OnDutyRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OnDutyRequests_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Payrolls] (
    [Id] nvarchar(450) NOT NULL,
    [CompanyId] nvarchar(max) NOT NULL,
    [BranchId] nvarchar(max) NULL,
    [EmployeeId] nvarchar(450) NOT NULL,
    [SalaryYear] int NOT NULL,
    [SalaryMonth] int NOT NULL,
    [SalaryDate] datetime2 NOT NULL,
    [GrossSalary] decimal(18,2) NOT NULL,
    [TotalEarnings] decimal(18,2) NOT NULL,
    [TotalDeductions] decimal(18,2) NOT NULL,
    [NetSalary] decimal(18,2) NOT NULL,
    [TotalWorkingDays] decimal(18,2) NULL,
    [PresentDays] decimal(18,2) NULL,
    [LeaveDays] decimal(18,2) NULL,
    [PaidLeaveDays] decimal(18,2) NULL,
    [UnpaidLeaveDays] decimal(18,2) NULL,
    [PayableDays] decimal(18,2) NULL,
    [ProrationBasisUsed] int NULL,
    [Status] nvarchar(max) NOT NULL,
    [RecalculatedCount] int NOT NULL,
    [LastRecalculatedOn] datetime2 NULL,
    [LastRecalculatedBy] nvarchar(max) NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Payrolls] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Payrolls_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ProbationConfirmations] (
    [Id] nvarchar(450) NOT NULL,
    [EmployeeId] nvarchar(450) NOT NULL,
    [ProbationStartDate] datetime2 NOT NULL,
    [OriginalProbationEndDate] datetime2 NOT NULL,
    [Recommendation] int NOT NULL,
    [ExtendedProbationEndDate] datetime2 NULL,
    [MakerId] nvarchar(max) NOT NULL,
    [MakerActionOn] datetime2 NOT NULL,
    [MakerRemarks] nvarchar(1000) NULL,
    [Status] int NOT NULL,
    [CheckerId] nvarchar(max) NULL,
    [CheckerActionOn] datetime2 NULL,
    [CheckerRemarks] nvarchar(1000) NULL,
    [FinalConfirmationDate] datetime2 NULL,
    [TenantId] nvarchar(450) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_ProbationConfirmations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProbationConfirmations_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [RejoiningHistories] (
    [Id] nvarchar(450) NOT NULL,
    [EmployeeId] nvarchar(450) NOT NULL,
    [PreviousRelievingDate] datetime2 NOT NULL,
    [PreviousJoiningDate] datetime2 NOT NULL,
    [NewJoiningDate] datetime2 NOT NULL,
    [Reason] nvarchar(max) NULL,
    [ProcessedByUserId] nvarchar(max) NOT NULL,
    [ProcessedOn] datetime2 NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_RejoiningHistories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RejoiningHistories_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [SalaryStructures] (
    [Id] nvarchar(450) NOT NULL,
    [EmployeeId] nvarchar(450) NOT NULL,
    [EffectiveFrom] datetime2 NOT NULL,
    [SourceTemplateId] nvarchar(450) NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_SalaryStructures] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SalaryStructures_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SalaryStructures_SalaryTemplates_SourceTemplateId] FOREIGN KEY ([SourceTemplateId]) REFERENCES [SalaryTemplates] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ShortLeaveRequests] (
    [Id] nvarchar(450) NOT NULL,
    [EmployeeId] nvarchar(450) NOT NULL,
    [LeaveTypeId] nvarchar(450) NOT NULL,
    [Date] datetime2 NOT NULL,
    [FromTime] time NOT NULL,
    [ToTime] time NOT NULL,
    [TotalHours] decimal(18,2) NOT NULL,
    [FractionalDays] decimal(18,2) NOT NULL,
    [Reason] nvarchar(max) NOT NULL,
    [Status] int NOT NULL,
    [ApprovedBy] nvarchar(max) NULL,
    [ApprovedOn] datetime2 NULL,
    [RejectedBy] nvarchar(max) NULL,
    [RejectedReason] nvarchar(max) NULL,
    [RejectedOn] datetime2 NULL,
    [CancelledOn] datetime2 NULL,
    [CancelledBy] nvarchar(max) NULL,
    [Remarks] nvarchar(500) NULL,
    [TenantId] nvarchar(450) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_ShortLeaveRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ShortLeaveRequests_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ShortLeaveRequests_LeaveTypes_LeaveTypeId] FOREIGN KEY ([LeaveTypeId]) REFERENCES [LeaveTypes] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [SupportTickets] (
    [Id] nvarchar(450) NOT NULL,
    [CompanyId] nvarchar(max) NULL,
    [BranchId] nvarchar(max) NULL,
    [EmployeeId] nvarchar(450) NOT NULL,
    [Subject] nvarchar(200) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [Category] int NOT NULL,
    [Priority] int NOT NULL,
    [Status] int NOT NULL,
    [AttachmentUrl] nvarchar(max) NULL,
    [AssignedToUserId] nvarchar(max) NULL,
    [ResolvedDate] datetime2 NULL,
    [ResolvedBy] nvarchar(max) NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_SupportTickets] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SupportTickets_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [TaxDeclarations] (
    [Id] nvarchar(450) NOT NULL,
    [EmployeeId] nvarchar(450) NOT NULL,
    [FinancialYearId] nvarchar(450) NOT NULL,
    [Regime] int NOT NULL,
    [Section80C] decimal(18,2) NOT NULL,
    [Section80CCD1B] decimal(18,2) NOT NULL,
    [Section80D] decimal(18,2) NOT NULL,
    [Section24B] decimal(18,2) NOT NULL,
    [OtherDeductions] decimal(18,2) NOT NULL,
    [AnnualRentPaid] decimal(18,2) NOT NULL,
    [IsMetroCity] bit NOT NULL,
    [LandlordPAN] nvarchar(1000) NULL,
    [Status] int NOT NULL,
    [SubmittedOn] datetime2 NULL,
    [VerifiedBy] nvarchar(max) NULL,
    [VerifiedOn] datetime2 NULL,
    [VerifierRemarks] nvarchar(1000) NULL,
    [TenantId] nvarchar(450) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_TaxDeclarations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TaxDeclarations_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TaxDeclarations_FinancialYears_FinancialYearId] FOREIGN KEY ([FinancialYearId]) REFERENCES [FinancialYears] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Users] (
    [Id] nvarchar(450) NOT NULL,
    [Username] nvarchar(100) NOT NULL,
    [Email] nvarchar(150) NULL,
    [PhoneNumber] nvarchar(15) NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [PasswordChangedOn] datetime2 NULL,
    [EmailConfirmed] bit NOT NULL,
    [PhoneConfirmed] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    [LockoutEnd] datetime2 NULL,
    [IsLocked] bit NOT NULL,
    [TenantId] nvarchar(450) NOT NULL,
    [CompanyId] nvarchar(450) NOT NULL,
    [BranchId] nvarchar(450) NULL,
    [EmployeeId] nvarchar(450) NULL,
    [LastLoginDate] datetime2 NULL,
    [LastLoginIP] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Users_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Users_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Users_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Users_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [WfhRequests] (
    [Id] nvarchar(450) NOT NULL,
    [EmployeeId] nvarchar(450) NOT NULL,
    [FromDate] datetime2 NOT NULL,
    [ToDate] datetime2 NOT NULL,
    [TotalDays] int NOT NULL,
    [Reason] nvarchar(max) NOT NULL,
    [Status] int NOT NULL,
    [ApprovedBy] nvarchar(max) NULL,
    [ApprovedOn] datetime2 NULL,
    [RejectedBy] nvarchar(max) NULL,
    [RejectedReason] nvarchar(max) NULL,
    [RejectedOn] datetime2 NULL,
    [CancelledOn] datetime2 NULL,
    [CancelledBy] nvarchar(max) NULL,
    [Remarks] nvarchar(500) NULL,
    [TenantId] nvarchar(450) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_WfhRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WfhRequests_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [CandidateApplications] (
    [Id] nvarchar(450) NOT NULL,
    [CandidateId] nvarchar(450) NOT NULL,
    [JobOpeningId] nvarchar(450) NOT NULL,
    [AppliedDate] datetime2 NOT NULL,
    [Status] int NOT NULL,
    [Remarks] nvarchar(max) NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_CandidateApplications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CandidateApplications_Candidates_CandidateId] FOREIGN KEY ([CandidateId]) REFERENCES [Candidates] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CandidateApplications_JobOpenings_JobOpeningId] FOREIGN KEY ([JobOpeningId]) REFERENCES [JobOpenings] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [AttendanceLogs] (
    [Id] nvarchar(450) NOT NULL,
    [AttendanceId] nvarchar(450) NOT NULL,
    [EmployeeId] nvarchar(450) NOT NULL,
    [PunchTime] datetime2 NOT NULL,
    [PunchType] int NOT NULL,
    [Browser] nvarchar(max) NULL,
    [Version] nvarchar(max) NULL,
    [OS] nvarchar(max) NULL,
    [DeviceType] nvarchar(max) NULL,
    [Location] nvarchar(max) NULL,
    [IsManual] bit NOT NULL,
    [DeviceId] nvarchar(max) NULL,
    [BiometricCode] nvarchar(max) NULL,
    [BiometricAttendanceLogId] nvarchar(450) NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_AttendanceLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AttendanceLogs_Attendances_AttendanceId] FOREIGN KEY ([AttendanceId]) REFERENCES [Attendances] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AttendanceLogs_BiometricAttendanceLogs_BiometricAttendanceLogId] FOREIGN KEY ([BiometricAttendanceLogId]) REFERENCES [BiometricAttendanceLogs] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AttendanceLogs_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [AttendanceRegularizations] (
    [Id] nvarchar(450) NOT NULL,
    [EmployeeId] nvarchar(450) NOT NULL,
    [Date] datetime2 NOT NULL,
    [AttendanceId] nvarchar(450) NULL,
    [OriginalFirstIn] datetime2 NULL,
    [OriginalLastOut] datetime2 NULL,
    [RequestedFirstIn] datetime2 NULL,
    [RequestedLastOut] datetime2 NULL,
    [Reason] nvarchar(max) NOT NULL,
    [Status] int NOT NULL,
    [CurrentLevel] int NOT NULL,
    [ApprovedBy] nvarchar(max) NULL,
    [ApprovedDate] datetime2 NULL,
    [RejectedReason] nvarchar(max) NULL,
    [SendBackReason] nvarchar(max) NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_AttendanceRegularizations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AttendanceRegularizations_Attendances_AttendanceId] FOREIGN KEY ([AttendanceId]) REFERENCES [Attendances] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AttendanceRegularizations_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [CompOffCandidates] (
    [Id] nvarchar(450) NOT NULL,
    [EmployeeId] nvarchar(450) NOT NULL,
    [AttendanceId] nvarchar(450) NOT NULL,
    [WorkedDate] datetime2 NOT NULL,
    [HoursWorked] decimal(18,2) NOT NULL,
    [TriggerReason] nvarchar(500) NOT NULL,
    [Status] int NOT NULL,
    [ReviewedBy] nvarchar(max) NULL,
    [ReviewedOn] datetime2 NULL,
    [RejectionReason] nvarchar(500) NULL,
    [CreditedDays] decimal(18,2) NOT NULL,
    [TenantId] nvarchar(450) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_CompOffCandidates] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CompOffCandidates_Attendances_AttendanceId] FOREIGN KEY ([AttendanceId]) REFERENCES [Attendances] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CompOffCandidates_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [LeaveApprovalHistories] (
    [Id] nvarchar(450) NOT NULL,
    [LeaveApplicationId] nvarchar(450) NOT NULL,
    [ActionBy] nvarchar(max) NOT NULL,
    [Action] int NOT NULL,
    [Remarks] nvarchar(max) NULL,
    [ActionDate] datetime2 NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LeaveApprovalHistories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LeaveApprovalHistories_LeaveApplications_LeaveApplicationId] FOREIGN KEY ([LeaveApplicationId]) REFERENCES [LeaveApplications] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [OnboardingChecklistItems] (
    [Id] nvarchar(450) NOT NULL,
    [OnboardingCaseId] nvarchar(450) NOT NULL,
    [StageType] int NOT NULL,
    [Title] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsMandatory] bit NOT NULL,
    [SortOrder] int NOT NULL,
    [Status] int NOT NULL,
    [CompletedOn] datetime2 NULL,
    [CompletedBy] nvarchar(max) NULL,
    [Remarks] nvarchar(max) NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_OnboardingChecklistItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OnboardingChecklistItems_OnboardingCases_OnboardingCaseId] FOREIGN KEY ([OnboardingCaseId]) REFERENCES [OnboardingCases] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [PayrollAuditLogs] (
    [Id] nvarchar(450) NOT NULL,
    [PayrollId] nvarchar(450) NOT NULL,
    [Action] nvarchar(max) NOT NULL,
    [OldNetSalary] decimal(18,2) NULL,
    [NewNetSalary] decimal(18,2) NULL,
    [OldPayableDays] decimal(18,2) NULL,
    [NewPayableDays] decimal(18,2) NULL,
    [PerformedBy] nvarchar(max) NOT NULL,
    [Remarks] nvarchar(500) NULL,
    [PerformedOn] datetime2 NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_PayrollAuditLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PayrollAuditLogs_Payrolls_PayrollId] FOREIGN KEY ([PayrollId]) REFERENCES [Payrolls] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [PayrollDetails] (
    [Id] nvarchar(450) NOT NULL,
    [PayrollId] nvarchar(450) NOT NULL,
    [SalaryComponentId] nvarchar(450) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [IsEarning] bit NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_PayrollDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PayrollDetails_Payrolls_PayrollId] FOREIGN KEY ([PayrollId]) REFERENCES [Payrolls] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PayrollDetails_SalaryComponents_SalaryComponentId] FOREIGN KEY ([SalaryComponentId]) REFERENCES [SalaryComponents] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [PayslipRequests] (
    [Id] nvarchar(450) NOT NULL,
    [EmployeeId] nvarchar(450) NOT NULL,
    [PayrollId] nvarchar(450) NOT NULL,
    [PayrollYear] int NOT NULL,
    [PayrollMonth] int NOT NULL,
    [Status] int NOT NULL,
    [ManagerRemarks] nvarchar(500) NULL,
    [ManagerActionBy] nvarchar(max) NULL,
    [ManagerActionOn] datetime2 NULL,
    [FinanceRemarks] nvarchar(500) NULL,
    [FinanceActionBy] nvarchar(max) NULL,
    [FinanceActionOn] datetime2 NULL,
    [DocumentUrl] nvarchar(max) NULL,
    [DocumentFileName] nvarchar(max) NULL,
    [GeneratedBy] nvarchar(max) NULL,
    [GeneratedOn] datetime2 NULL,
    [CompletedBy] nvarchar(max) NULL,
    [CompletedOn] datetime2 NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_PayslipRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PayslipRequests_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PayslipRequests_Payrolls_PayrollId] FOREIGN KEY ([PayrollId]) REFERENCES [Payrolls] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Payslips] (
    [Id] nvarchar(450) NOT NULL,
    [PayrollId] nvarchar(450) NOT NULL,
    [GeneratedDate] datetime2 NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Payslips] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Payslips_Payrolls_PayrollId] FOREIGN KEY ([PayrollId]) REFERENCES [Payrolls] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [PipRecords] (
    [Id] nvarchar(450) NOT NULL,
    [EmployeeId] nvarchar(450) NOT NULL,
    [ProbationConfirmationId] nvarchar(450) NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [Goals] nvarchar(max) NOT NULL,
    [MidReviewDate] datetime2 NULL,
    [MidReviewNotes] nvarchar(2000) NULL,
    [FinalOutcome] int NOT NULL,
    [ProposedFinalOutcome] int NULL,
    [MakerId] nvarchar(max) NULL,
    [MakerActionOn] datetime2 NULL,
    [MakerRemarks] nvarchar(1000) NULL,
    [Status] int NOT NULL,
    [CheckerId] nvarchar(max) NULL,
    [CheckerActionOn] datetime2 NULL,
    [CheckerRemarks] nvarchar(1000) NULL,
    [TenantId] nvarchar(450) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_PipRecords] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PipRecords_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PipRecords_ProbationConfirmations_ProbationConfirmationId] FOREIGN KEY ([ProbationConfirmationId]) REFERENCES [ProbationConfirmations] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [SalaryDetails] (
    [Id] nvarchar(450) NOT NULL,
    [SalaryStructureId] nvarchar(450) NOT NULL,
    [SalaryComponentId] nvarchar(450) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_SalaryDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SalaryDetails_SalaryComponents_SalaryComponentId] FOREIGN KEY ([SalaryComponentId]) REFERENCES [SalaryComponents] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SalaryDetails_SalaryStructures_SalaryStructureId] FOREIGN KEY ([SalaryStructureId]) REFERENCES [SalaryStructures] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [SupportTicketReplies] (
    [Id] nvarchar(450) NOT NULL,
    [SupportTicketId] nvarchar(450) NOT NULL,
    [RepliedByUserId] nvarchar(max) NOT NULL,
    [Message] nvarchar(max) NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_SupportTicketReplies] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SupportTicketReplies_SupportTickets_SupportTicketId] FOREIGN KEY ([SupportTicketId]) REFERENCES [SupportTickets] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [EmployeeTaxComputations] (
    [Id] nvarchar(450) NOT NULL,
    [EmployeeId] nvarchar(450) NOT NULL,
    [FinancialYearId] nvarchar(450) NOT NULL,
    [TaxDeclarationId] nvarchar(450) NULL,
    [Regime] int NOT NULL,
    [AnnualGrossSalary] decimal(18,2) NOT NULL,
    [StandardDeduction] decimal(18,2) NOT NULL,
    [HraExemption] decimal(18,2) NOT NULL,
    [TotalChapterVIADeductions] decimal(18,2) NOT NULL,
    [TaxableIncome] decimal(18,2) NOT NULL,
    [TaxBeforeCess] decimal(18,2) NOT NULL,
    [Rebate87A] decimal(18,2) NOT NULL,
    [HealthEducationCess] decimal(18,2) NOT NULL,
    [AnnualTaxLiability] decimal(18,2) NOT NULL,
    [TdsDeductedTillDate] decimal(18,2) NOT NULL,
    [MonthlyTdsForRemainingMonths] decimal(18,2) NOT NULL,
    [ComputedOn] datetime2 NOT NULL,
    [ComputedBy] nvarchar(max) NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_EmployeeTaxComputations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmployeeTaxComputations_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_EmployeeTaxComputations_FinancialYears_FinancialYearId] FOREIGN KEY ([FinancialYearId]) REFERENCES [FinancialYears] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_EmployeeTaxComputations_TaxDeclarations_TaxDeclarationId] FOREIGN KEY ([TaxDeclarationId]) REFERENCES [TaxDeclarations] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [EmployeeAdvances] (
    [Id] nvarchar(450) NOT NULL,
    [TenantId] nvarchar(450) NOT NULL,
    [CompanyId] nvarchar(450) NULL,
    [BranchId] nvarchar(450) NULL,
    [EmployeeId] nvarchar(450) NOT NULL,
    [AdvanceTypeId] nvarchar(450) NOT NULL,
    [RequestedAmount] decimal(18,2) NOT NULL,
    [ApprovedAmount] decimal(18,2) NULL,
    [InstallmentCount] int NOT NULL,
    [Purpose] nvarchar(500) NULL,
    [Status] int NOT NULL,
    [CurrentApprovalLevel] int NOT NULL,
    [MakerId] nvarchar(450) NOT NULL,
    [MakerActionOn] datetime2 NOT NULL,
    [MakerRemarks] nvarchar(1000) NULL,
    [DisbursedAmount] decimal(18,2) NULL,
    [DisbursedOn] datetime2 NULL,
    [DisbursementMode] int NULL,
    [DisbursementReference] nvarchar(100) NULL,
    [OutstandingAmount] decimal(18,2) NOT NULL,
    [ClosedOn] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_EmployeeAdvances] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmployeeAdvances_AdvanceTypes_AdvanceTypeId] FOREIGN KEY ([AdvanceTypeId]) REFERENCES [AdvanceTypes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_EmployeeAdvances_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_EmployeeAdvances_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_EmployeeAdvances_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_EmployeeAdvances_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_EmployeeAdvances_Users_MakerId] FOREIGN KEY ([MakerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [EmployeeLoans] (
    [Id] nvarchar(450) NOT NULL,
    [TenantId] nvarchar(450) NOT NULL,
    [CompanyId] nvarchar(450) NULL,
    [BranchId] nvarchar(450) NULL,
    [EmployeeId] nvarchar(450) NOT NULL,
    [LoanTypeId] nvarchar(450) NOT NULL,
    [LoanPolicyId] nvarchar(450) NOT NULL,
    [RequestedAmount] decimal(18,2) NOT NULL,
    [ApprovedAmount] decimal(18,2) NULL,
    [TenureMonths] int NOT NULL,
    [InterestRatePercent] decimal(18,2) NOT NULL,
    [InterestMethod] int NOT NULL,
    [Purpose] nvarchar(500) NULL,
    [Status] int NOT NULL,
    [CurrentApprovalLevel] int NOT NULL,
    [MakerId] nvarchar(450) NOT NULL,
    [MakerActionOn] datetime2 NOT NULL,
    [MakerRemarks] nvarchar(1000) NULL,
    [DisbursedAmount] decimal(18,2) NULL,
    [DisbursedOn] datetime2 NULL,
    [DisbursementMode] int NULL,
    [DisbursementReference] nvarchar(100) NULL,
    [OutstandingPrincipal] decimal(18,2) NOT NULL,
    [ClosedOn] datetime2 NULL,
    [ClosureReason] int NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_EmployeeLoans] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmployeeLoans_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_EmployeeLoans_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_EmployeeLoans_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_EmployeeLoans_LoanPolicies_LoanPolicyId] FOREIGN KEY ([LoanPolicyId]) REFERENCES [LoanPolicies] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_EmployeeLoans_LoanTypes_LoanTypeId] FOREIGN KEY ([LoanTypeId]) REFERENCES [LoanTypes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_EmployeeLoans_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_EmployeeLoans_Users_MakerId] FOREIGN KEY ([MakerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [LoanAdvanceAttachments] (
    [Id] nvarchar(450) NOT NULL,
    [EntityType] int NOT NULL,
    [EntityId] nvarchar(450) NOT NULL,
    [FileName] nvarchar(255) NOT NULL,
    [FilePath] nvarchar(500) NOT NULL,
    [ContentType] nvarchar(100) NOT NULL,
    [FileSizeBytes] bigint NOT NULL,
    [UploadedBy] nvarchar(max) NOT NULL,
    [UploadedByUserId] nvarchar(450) NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LoanAdvanceAttachments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LoanAdvanceAttachments_Users_UploadedByUserId] FOREIGN KEY ([UploadedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [LoanAdvanceAuditLogs] (
    [Id] nvarchar(450) NOT NULL,
    [EntityType] nvarchar(450) NOT NULL,
    [EntityId] nvarchar(450) NOT NULL,
    [Action] nvarchar(max) NOT NULL,
    [OldValuesJson] nvarchar(max) NULL,
    [NewValuesJson] nvarchar(max) NULL,
    [PerformedBy] nvarchar(max) NOT NULL,
    [PerformedByUserId] nvarchar(450) NULL,
    [PerformedOn] datetime2 NOT NULL,
    [IpAddress] nvarchar(max) NULL,
    [TenantId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_LoanAdvanceAuditLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LoanAdvanceAuditLogs_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LoanAdvanceAuditLogs_Users_PerformedByUserId] FOREIGN KEY ([PerformedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [LoanPolicyApprovalLevels] (
    [Id] nvarchar(450) NOT NULL,
    [LoanPolicyId] nvarchar(450) NOT NULL,
    [LevelNumber] int NOT NULL,
    [ApproverType] int NOT NULL,
    [ApproverRoleId] nvarchar(450) NULL,
    [ApproverUserId] nvarchar(450) NULL,
    [MinAmountThreshold] decimal(18,2) NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LoanPolicyApprovalLevels] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LoanPolicyApprovalLevels_LoanPolicies_LoanPolicyId] FOREIGN KEY ([LoanPolicyId]) REFERENCES [LoanPolicies] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LoanPolicyApprovalLevels_Roles_ApproverRoleId] FOREIGN KEY ([ApproverRoleId]) REFERENCES [Roles] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LoanPolicyApprovalLevels_Users_ApproverUserId] FOREIGN KEY ([ApproverUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [LoginHistories] (
    [Id] nvarchar(450) NOT NULL,
    [TenantId] nvarchar(450) NOT NULL,
    [UserId] nvarchar(450) NOT NULL,
    [SessionId] nvarchar(max) NOT NULL,
    [LoginTime] datetime2 NOT NULL,
    [LogoutTime] datetime2 NULL,
    [LoginStatus] int NOT NULL,
    [FailureReason] nvarchar(max) NULL,
    [IPAddress] nvarchar(max) NOT NULL,
    [DeviceInfo] nvarchar(max) NOT NULL,
    [Browser] nvarchar(max) NOT NULL,
    [OS] nvarchar(max) NOT NULL,
    [Country] nvarchar(max) NULL,
    [City] nvarchar(max) NULL,
    [IsSuspicious] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LoginHistories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LoginHistories_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LoginHistories_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [NotificationGroupUsers] (
    [Id] nvarchar(450) NOT NULL,
    [NotificationGroupId] nvarchar(450) NOT NULL,
    [UserId] nvarchar(450) NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_NotificationGroupUsers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_NotificationGroupUsers_NotificationGroups_NotificationGroupId] FOREIGN KEY ([NotificationGroupId]) REFERENCES [NotificationGroups] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_NotificationGroupUsers_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [NotificationRecipients] (
    [Id] nvarchar(450) NOT NULL,
    [NotificationId] nvarchar(450) NOT NULL,
    [UserId] nvarchar(450) NOT NULL,
    [IsRead] bit NOT NULL,
    [ReadDate] datetime2 NULL,
    [IsDelivered] bit NOT NULL,
    [DeliveredDate] datetime2 NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_NotificationRecipients] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_NotificationRecipients_Notifications_NotificationId] FOREIGN KEY ([NotificationId]) REFERENCES [Notifications] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_NotificationRecipients_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [RefreshTokens] (
    [Id] nvarchar(450) NOT NULL,
    [UserId] nvarchar(450) NOT NULL,
    [Token] nvarchar(max) NOT NULL,
    [ExpiryDate] datetime2 NOT NULL,
    [IsRevoked] bit NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [UserFavoriteMenus] (
    [Id] nvarchar(450) NOT NULL,
    [UserId] nvarchar(450) NOT NULL,
    [AppFeatureId] nvarchar(450) NOT NULL,
    [DisplayOrder] int NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_UserFavoriteMenus] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserFavoriteMenus_AppFeatures_AppFeatureId] FOREIGN KEY ([AppFeatureId]) REFERENCES [AppFeatures] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_UserFavoriteMenus_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [UserRoles] (
    [Id] nvarchar(450) NOT NULL,
    [UserId] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_UserRoles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [InterviewSchedules] (
    [Id] nvarchar(450) NOT NULL,
    [CandidateApplicationId] nvarchar(450) NOT NULL,
    [InterviewDate] datetime2 NOT NULL,
    [InterviewerId] nvarchar(450) NOT NULL,
    [Mode] int NOT NULL,
    [Status] int NOT NULL,
    [Feedback] nvarchar(max) NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_InterviewSchedules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InterviewSchedules_CandidateApplications_CandidateApplicationId] FOREIGN KEY ([CandidateApplicationId]) REFERENCES [CandidateApplications] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InterviewSchedules_Employees_InterviewerId] FOREIGN KEY ([InterviewerId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [AttendanceRegularizationApprovalHistories] (
    [Id] nvarchar(450) NOT NULL,
    [AttendanceRegularizationId] nvarchar(450) NOT NULL,
    [ActionBy] nvarchar(max) NOT NULL,
    [Action] int NOT NULL,
    [Remarks] nvarchar(max) NULL,
    [ActionDate] datetime2 NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_AttendanceRegularizationApprovalHistories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AttendanceRegularizationApprovalHistories_AttendanceRegularizations_AttendanceRegularizationId] FOREIGN KEY ([AttendanceRegularizationId]) REFERENCES [AttendanceRegularizations] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [PayslipRequestAudits] (
    [Id] nvarchar(450) NOT NULL,
    [PayslipRequestId] nvarchar(450) NOT NULL,
    [Action] nvarchar(max) NOT NULL,
    [PerformedBy] nvarchar(max) NOT NULL,
    [Remarks] nvarchar(500) NULL,
    [PerformedOn] datetime2 NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_PayslipRequestAudits] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PayslipRequestAudits_PayslipRequests_PayslipRequestId] FOREIGN KEY ([PayslipRequestId]) REFERENCES [PayslipRequests] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [AdvanceApprovalHistories] (
    [Id] nvarchar(450) NOT NULL,
    [EmployeeAdvanceId] nvarchar(450) NOT NULL,
    [LevelNumber] int NOT NULL,
    [CheckerId] nvarchar(450) NOT NULL,
    [ActedAsDelegateForUserId] nvarchar(450) NULL,
    [Decision] int NOT NULL,
    [Remarks] nvarchar(1000) NULL,
    [ActionOn] datetime2 NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_AdvanceApprovalHistories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AdvanceApprovalHistories_EmployeeAdvances_EmployeeAdvanceId] FOREIGN KEY ([EmployeeAdvanceId]) REFERENCES [EmployeeAdvances] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AdvanceApprovalHistories_Users_ActedAsDelegateForUserId] FOREIGN KEY ([ActedAsDelegateForUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AdvanceApprovalHistories_Users_CheckerId] FOREIGN KEY ([CheckerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [AdvanceInstallments] (
    [Id] nvarchar(450) NOT NULL,
    [EmployeeAdvanceId] nvarchar(450) NOT NULL,
    [InstallmentNumber] int NOT NULL,
    [DueDate] datetime2 NOT NULL,
    [InstallmentAmount] decimal(18,2) NOT NULL,
    [Status] int NOT NULL,
    [RecoveredOn] datetime2 NULL,
    [PayrollId] nvarchar(450) NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_AdvanceInstallments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AdvanceInstallments_EmployeeAdvances_EmployeeAdvanceId] FOREIGN KEY ([EmployeeAdvanceId]) REFERENCES [EmployeeAdvances] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AdvanceInstallments_Payrolls_PayrollId] FOREIGN KEY ([PayrollId]) REFERENCES [Payrolls] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [LoanApprovalHistories] (
    [Id] nvarchar(450) NOT NULL,
    [EmployeeLoanId] nvarchar(450) NOT NULL,
    [LevelNumber] int NOT NULL,
    [CheckerId] nvarchar(450) NOT NULL,
    [ActedAsDelegateForUserId] nvarchar(450) NULL,
    [Decision] int NOT NULL,
    [Remarks] nvarchar(1000) NULL,
    [ActionOn] datetime2 NOT NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LoanApprovalHistories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LoanApprovalHistories_EmployeeLoans_EmployeeLoanId] FOREIGN KEY ([EmployeeLoanId]) REFERENCES [EmployeeLoans] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LoanApprovalHistories_Users_ActedAsDelegateForUserId] FOREIGN KEY ([ActedAsDelegateForUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LoanApprovalHistories_Users_CheckerId] FOREIGN KEY ([CheckerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [LoanEmiSchedules] (
    [Id] nvarchar(450) NOT NULL,
    [EmployeeLoanId] nvarchar(450) NOT NULL,
    [InstallmentNumber] int NOT NULL,
    [DueDate] datetime2 NOT NULL,
    [OpeningBalance] decimal(18,2) NOT NULL,
    [PrincipalComponent] decimal(18,2) NOT NULL,
    [InterestComponent] decimal(18,2) NOT NULL,
    [EmiAmount] decimal(18,2) NOT NULL,
    [ClosingBalance] decimal(18,2) NOT NULL,
    [Status] int NOT NULL,
    [RecoveredOn] datetime2 NULL,
    [PayrollId] nvarchar(450) NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LoanEmiSchedules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LoanEmiSchedules_EmployeeLoans_EmployeeLoanId] FOREIGN KEY ([EmployeeLoanId]) REFERENCES [EmployeeLoans] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LoanEmiSchedules_Payrolls_PayrollId] FOREIGN KEY ([PayrollId]) REFERENCES [Payrolls] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [AdvancePaymentHistories] (
    [Id] nvarchar(450) NOT NULL,
    [EmployeeAdvanceId] nvarchar(450) NOT NULL,
    [AdvanceInstallmentId] nvarchar(450) NULL,
    [PaymentSource] int NOT NULL,
    [AmountPaid] decimal(18,2) NOT NULL,
    [PaymentDate] datetime2 NOT NULL,
    [PayrollId] nvarchar(450) NULL,
    [ReceiptReference] nvarchar(100) NULL,
    [Remarks] nvarchar(500) NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_AdvancePaymentHistories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AdvancePaymentHistories_AdvanceInstallments_AdvanceInstallmentId] FOREIGN KEY ([AdvanceInstallmentId]) REFERENCES [AdvanceInstallments] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AdvancePaymentHistories_EmployeeAdvances_EmployeeAdvanceId] FOREIGN KEY ([EmployeeAdvanceId]) REFERENCES [EmployeeAdvances] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AdvancePaymentHistories_Payrolls_PayrollId] FOREIGN KEY ([PayrollId]) REFERENCES [Payrolls] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [LoanPaymentHistories] (
    [Id] nvarchar(450) NOT NULL,
    [EmployeeLoanId] nvarchar(450) NOT NULL,
    [LoanEmiScheduleId] nvarchar(450) NULL,
    [PaymentSource] int NOT NULL,
    [AmountPaid] decimal(18,2) NOT NULL,
    [PrincipalPaid] decimal(18,2) NOT NULL,
    [InterestPaid] decimal(18,2) NOT NULL,
    [PaymentDate] datetime2 NOT NULL,
    [PayrollId] nvarchar(450) NULL,
    [ReceiptReference] nvarchar(100) NULL,
    [Remarks] nvarchar(500) NULL,
    [TenantId] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_LoanPaymentHistories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LoanPaymentHistories_EmployeeLoans_EmployeeLoanId] FOREIGN KEY ([EmployeeLoanId]) REFERENCES [EmployeeLoans] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LoanPaymentHistories_LoanEmiSchedules_LoanEmiScheduleId] FOREIGN KEY ([LoanEmiScheduleId]) REFERENCES [LoanEmiSchedules] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LoanPaymentHistories_Payrolls_PayrollId] FOREIGN KEY ([PayrollId]) REFERENCES [Payrolls] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_AdvanceApprovalHistories_ActedAsDelegateForUserId] ON [AdvanceApprovalHistories] ([ActedAsDelegateForUserId]);

CREATE INDEX [IX_AdvanceApprovalHistories_Advance] ON [AdvanceApprovalHistories] ([EmployeeAdvanceId], [LevelNumber]);

CREATE INDEX [IX_AdvanceApprovalHistories_CheckerId] ON [AdvanceApprovalHistories] ([CheckerId]);

CREATE INDEX [IX_AdvanceInstallments_DueTracking] ON [AdvanceInstallments] ([Status], [DueDate]);

CREATE INDEX [IX_AdvanceInstallments_PayrollId] ON [AdvanceInstallments] ([PayrollId]);

CREATE UNIQUE INDEX [UX_AdvanceInstallments] ON [AdvanceInstallments] ([EmployeeAdvanceId], [InstallmentNumber]);

CREATE INDEX [IX_AdvancePaymentHistories_Advance_Date] ON [AdvancePaymentHistories] ([EmployeeAdvanceId], [PaymentDate]);

CREATE INDEX [IX_AdvancePaymentHistories_AdvanceInstallmentId] ON [AdvancePaymentHistories] ([AdvanceInstallmentId]);

CREATE INDEX [IX_AdvancePaymentHistories_PaymentDate] ON [AdvancePaymentHistories] ([PaymentDate]);

CREATE INDEX [IX_AdvancePaymentHistories_PayrollId] ON [AdvancePaymentHistories] ([PayrollId]);

CREATE UNIQUE INDEX [UX_AdvanceTypes_Tenant_Code] ON [AdvanceTypes] ([TenantId], [Code]) WHERE [IsDeleted] = 0;

CREATE INDEX [IX_Announcements_DepartmentId] ON [Announcements] ([DepartmentId]);

CREATE INDEX [IX_Announcements_PublishDate_ExpiryDate] ON [Announcements] ([PublishDate], [ExpiryDate]);

CREATE INDEX [IX_Announcements_RoleId] ON [Announcements] ([RoleId]);

CREATE INDEX [IX_ApiResponseLogs_CompanyId] ON [ApiResponseLogs] ([CompanyId]);

CREATE INDEX [IX_ApiResponseLogs_CorrelationId] ON [ApiResponseLogs] ([CorrelationId]);

CREATE UNIQUE INDEX [IX_ApiResponseLogs_Id] ON [ApiResponseLogs] ([Id]);

CREATE INDEX [IX_ApiResponseLogs_RequestId] ON [ApiResponseLogs] ([RequestId]);

CREATE INDEX [IX_ApiResponseLogs_ResponseTime] ON [ApiResponseLogs] ([ResponseTime]);

CREATE INDEX [IX_ApiResponseLogs_UserId] ON [ApiResponseLogs] ([UserId]);

CREATE INDEX [IX_AppFeatures_ParentFeatureId] ON [AppFeatures] ([ParentFeatureId]);

CREATE INDEX [IX_ApprovalDelegations_DelegateEmployeeId] ON [ApprovalDelegations] ([DelegateEmployeeId]);

CREATE INDEX [IX_ApprovalDelegations_DelegatorEmployeeId_StartDate_EndDate] ON [ApprovalDelegations] ([DelegatorEmployeeId], [StartDate], [EndDate]);

CREATE INDEX [IX_AssetAllocations_AssetId] ON [AssetAllocations] ([AssetId]);

CREATE INDEX [IX_AssetAllocations_EmployeeId] ON [AssetAllocations] ([EmployeeId]);

CREATE INDEX [IX_AssetHistories_AssetId] ON [AssetHistories] ([AssetId]);

CREATE INDEX [IX_Assets_AssetCategoryId] ON [Assets] ([AssetCategoryId]);

CREATE UNIQUE INDEX [IX_Assets_AssetCode] ON [Assets] ([AssetCode]);

CREATE INDEX [IX_AttendanceLogs_AttendanceId] ON [AttendanceLogs] ([AttendanceId]);

CREATE UNIQUE INDEX [IX_AttendanceLogs_BiometricAttendanceLogId] ON [AttendanceLogs] ([BiometricAttendanceLogId]) WHERE [BiometricAttendanceLogId] IS NOT NULL;

CREATE INDEX [IX_AttendanceLogs_EmployeeId_PunchTime] ON [AttendanceLogs] ([EmployeeId], [PunchTime]);

CREATE INDEX [IX_AttendancePolicies_CompanyId] ON [AttendancePolicies] ([CompanyId]);

CREATE INDEX [IX_AttendancePolicies_TenantId] ON [AttendancePolicies] ([TenantId]);

CREATE INDEX [IX_AttendancePolicies_TenantId_CompanyId_IsActive_EffectiveFrom] ON [AttendancePolicies] ([TenantId], [CompanyId], [IsActive], [EffectiveFrom]);

CREATE INDEX [IX_AttendanceRegularizationApprovalHistories_AttendanceRegularizationId] ON [AttendanceRegularizationApprovalHistories] ([AttendanceRegularizationId]);

CREATE INDEX [IX_AttendanceRegularizations_AttendanceId] ON [AttendanceRegularizations] ([AttendanceId]);

CREATE INDEX [IX_AttendanceRegularizations_EmployeeId_Date] ON [AttendanceRegularizations] ([EmployeeId], [Date]);

CREATE INDEX [IX_Attendances_BranchId] ON [Attendances] ([BranchId]);

CREATE INDEX [IX_Attendances_CompanyId] ON [Attendances] ([CompanyId]);

CREATE INDEX [IX_Attendances_EmployeeId_Date] ON [Attendances] ([EmployeeId], [Date]);

CREATE INDEX [IX_Attendances_ShiftId] ON [Attendances] ([ShiftId]);

CREATE INDEX [IX_Attendances_TenantId] ON [Attendances] ([TenantId]);

CREATE INDEX [IX_BiometricAgents_BranchId] ON [BiometricAgents] ([BranchId]);

CREATE UNIQUE INDEX [IX_BiometricAgents_Tenant_AgentCode] ON [BiometricAgents] ([TenantId], [AgentCode]);

CREATE UNIQUE INDEX [IX_BiometricAttendanceLogs_Device_TransactionId] ON [BiometricAttendanceLogs] ([DeviceId], [DeviceTransactionId]) WHERE [DeviceTransactionId] IS NOT NULL;

CREATE UNIQUE INDEX [IX_BiometricAttendanceLogs_Tenant_Device_Employee_PunchTime] ON [BiometricAttendanceLogs] ([TenantId], [DeviceId], [EmployeeCode], [PunchTime]) WHERE [TenantId] IS NOT NULL;

CREATE INDEX [IX_BiometricAttendanceLogs_Tenant_IsProcessed] ON [BiometricAttendanceLogs] ([TenantId], [IsProcessed]);

CREATE INDEX [IX_BiometricDevices_AgentId] ON [BiometricDevices] ([AgentId]);

CREATE INDEX [IX_BiometricDevices_BranchId] ON [BiometricDevices] ([BranchId]);

CREATE INDEX [IX_BiometricDevices_CompanyId] ON [BiometricDevices] ([CompanyId]);

CREATE UNIQUE INDEX [IX_BiometricDevices_Tenant_DeviceCode] ON [BiometricDevices] ([TenantId], [DeviceCode]);

CREATE INDEX [IX_BiometricDeviceTestRequests_Agent_Status] ON [BiometricDeviceTestRequests] ([AgentId], [Status]);

CREATE INDEX [IX_BiometricDeviceTestRequests_Device_RequestedOn] ON [BiometricDeviceTestRequests] ([DeviceId], [RequestedOn]);

CREATE INDEX [IX_BiometricDeviceTestRequests_TenantId] ON [BiometricDeviceTestRequests] ([TenantId]);

CREATE INDEX [IX_BiometricSyncLogs_Device_StartTime] ON [BiometricSyncLogs] ([DeviceId], [StartTime]);

CREATE INDEX [IX_BiometricSyncLogs_Tenant_SyncType_StartTime] ON [BiometricSyncLogs] ([TenantId], [SyncType], [StartTime]);

CREATE INDEX [IX_Branches_CityId] ON [Branches] ([CityId]);

CREATE INDEX [IX_Branches_CompanyId] ON [Branches] ([CompanyId]);

CREATE INDEX [IX_Branches_CountryId] ON [Branches] ([CountryId]);

CREATE INDEX [IX_Branches_StateId] ON [Branches] ([StateId]);

CREATE INDEX [IX_Branches_TenantId] ON [Branches] ([TenantId]);

CREATE INDEX [IX_CandidateApplications_CandidateId] ON [CandidateApplications] ([CandidateId]);

CREATE INDEX [IX_CandidateApplications_JobOpeningId] ON [CandidateApplications] ([JobOpeningId]);

CREATE INDEX [IX_Cities_StateId] ON [Cities] ([StateId]);

CREATE INDEX [IX_Companies_CityId] ON [Companies] ([CityId]);

CREATE INDEX [IX_Companies_CountryId] ON [Companies] ([CountryId]);

CREATE INDEX [IX_Companies_StateId] ON [Companies] ([StateId]);

CREATE INDEX [IX_Companies_TenantId] ON [Companies] ([TenantId]);

CREATE INDEX [IX_CompOffCandidates_AttendanceId] ON [CompOffCandidates] ([AttendanceId]);

CREATE INDEX [IX_CompOffCandidates_EmployeeId] ON [CompOffCandidates] ([EmployeeId]);

CREATE UNIQUE INDEX [IX_CompOffCandidates_EmployeeId_AttendanceId] ON [CompOffCandidates] ([EmployeeId], [AttendanceId]);

CREATE INDEX [IX_CompOffCandidates_TenantId_Status] ON [CompOffCandidates] ([TenantId], [Status]);

CREATE INDEX [IX_Departments_BranchId] ON [Departments] ([BranchId]);

CREATE INDEX [IX_Departments_CompanyId] ON [Departments] ([CompanyId]);

CREATE INDEX [IX_Departments_ParentDepartmentId] ON [Departments] ([ParentDepartmentId]);

CREATE INDEX [IX_Departments_TenantId] ON [Departments] ([TenantId]);

CREATE INDEX [IX_Designations_BranchId] ON [Designations] ([BranchId]);

CREATE INDEX [IX_Designations_CompanyId] ON [Designations] ([CompanyId]);

CREATE INDEX [IX_Designations_DepartmentId] ON [Designations] ([DepartmentId]);

CREATE INDEX [IX_Designations_ParentDesignationId] ON [Designations] ([ParentDesignationId]);

CREATE INDEX [IX_Designations_TenantId] ON [Designations] ([TenantId]);

CREATE INDEX [IX_EmployeeAdvances_AdvanceTypeId] ON [EmployeeAdvances] ([AdvanceTypeId]);

CREATE INDEX [IX_EmployeeAdvances_BranchId] ON [EmployeeAdvances] ([BranchId]);

CREATE INDEX [IX_EmployeeAdvances_CompanyId] ON [EmployeeAdvances] ([CompanyId]);

CREATE INDEX [IX_EmployeeAdvances_Employee_Status] ON [EmployeeAdvances] ([EmployeeId], [Status]);

CREATE INDEX [IX_EmployeeAdvances_MakerId] ON [EmployeeAdvances] ([MakerId]);

CREATE INDEX [IX_EmployeeAdvances_Tenant_Company_Branch] ON [EmployeeAdvances] ([TenantId], [CompanyId], [BranchId]);

CREATE INDEX [IX_EmployeeAdvances_Tenant_Status] ON [EmployeeAdvances] ([TenantId], [Status]);

CREATE INDEX [IX_EmployeeBankDetails_EmployeeId] ON [EmployeeBankDetails] ([EmployeeId]);

CREATE INDEX [IX_EmployeeBiometricMappings_EmployeeId] ON [EmployeeBiometricMappings] ([EmployeeId]);

CREATE INDEX [IX_EmployeeDocuments_EmployeeId] ON [EmployeeDocuments] ([EmployeeId]);

CREATE INDEX [IX_EmployeeEducationDetails_EmployeeId] ON [EmployeeEducationDetails] ([EmployeeId]);

CREATE INDEX [IX_EmployeeESICDetails_EmployeeId] ON [EmployeeESICDetails] ([EmployeeId]);

CREATE INDEX [IX_EmployeeFeedbacks_EmployeeId] ON [EmployeeFeedbacks] ([EmployeeId]);

CREATE INDEX [IX_EmployeeFeedbacks_TenantId_EmployeeId] ON [EmployeeFeedbacks] ([TenantId], [EmployeeId]);

CREATE INDEX [IX_EmployeeLoans_BranchId] ON [EmployeeLoans] ([BranchId]);

CREATE INDEX [IX_EmployeeLoans_CompanyId] ON [EmployeeLoans] ([CompanyId]);

CREATE INDEX [IX_EmployeeLoans_Employee_Status] ON [EmployeeLoans] ([EmployeeId], [Status]);

CREATE INDEX [IX_EmployeeLoans_LoanPolicyId] ON [EmployeeLoans] ([LoanPolicyId]);

CREATE INDEX [IX_EmployeeLoans_LoanTypeId] ON [EmployeeLoans] ([LoanTypeId]);

CREATE INDEX [IX_EmployeeLoans_MakerId] ON [EmployeeLoans] ([MakerId]);

CREATE INDEX [IX_EmployeeLoans_Tenant_Company_Branch] ON [EmployeeLoans] ([TenantId], [CompanyId], [BranchId]);

CREATE INDEX [IX_EmployeeLoans_Tenant_Status] ON [EmployeeLoans] ([TenantId], [Status]);

CREATE INDEX [IX_EmployeePFDetails_EmployeeId] ON [EmployeePFDetails] ([EmployeeId]);

CREATE INDEX [IX_Employees_BranchId] ON [Employees] ([BranchId]);

CREATE INDEX [IX_Employees_CompanyId] ON [Employees] ([CompanyId]);

CREATE INDEX [IX_Employees_CountryId] ON [Employees] ([CountryId]);

CREATE INDEX [IX_Employees_DepartmentId] ON [Employees] ([DepartmentId]);

CREATE INDEX [IX_Employees_DesignationId] ON [Employees] ([DesignationId]);

CREATE UNIQUE INDEX [IX_Employees_EmployeeCode] ON [Employees] ([EmployeeCode]);

CREATE INDEX [IX_Employees_ReportingManagerId] ON [Employees] ([ReportingManagerId]);

CREATE INDEX [IX_Employees_ShiftId] ON [Employees] ([ShiftId]);

CREATE INDEX [IX_Employees_TenantId] ON [Employees] ([TenantId]);

CREATE INDEX [IX_EmployeeShiftMappings_EmployeeId] ON [EmployeeShiftMappings] ([EmployeeId]);

CREATE INDEX [IX_EmployeeShiftMappings_ShiftId] ON [EmployeeShiftMappings] ([ShiftId]);

CREATE INDEX [IX_EmployeeTasks_EmployeeId_Status] ON [EmployeeTasks] ([EmployeeId], [Status]);

CREATE UNIQUE INDEX [IX_EmployeeTaxComputations_EmployeeId_FinancialYearId] ON [EmployeeTaxComputations] ([EmployeeId], [FinancialYearId]);

CREATE INDEX [IX_EmployeeTaxComputations_FinancialYearId] ON [EmployeeTaxComputations] ([FinancialYearId]);

CREATE INDEX [IX_EmployeeTaxComputations_TaxDeclarationId] ON [EmployeeTaxComputations] ([TaxDeclarationId]);

CREATE INDEX [IX_EmployeeTransfers_EmployeeId] ON [EmployeeTransfers] ([EmployeeId]);

CREATE INDEX [IX_EmployeeTransfers_TenantId_Status] ON [EmployeeTransfers] ([TenantId], [Status]);

CREATE INDEX [IX_ErrorLogs_CompanyId] ON [ErrorLogs] ([CompanyId]);

CREATE INDEX [IX_ErrorLogs_CorrelationId] ON [ErrorLogs] ([CorrelationId]);

CREATE INDEX [IX_ErrorLogs_ErrorTime] ON [ErrorLogs] ([ErrorTime]);

CREATE UNIQUE INDEX [IX_ErrorLogs_Id] ON [ErrorLogs] ([Id]);

CREATE INDEX [IX_ErrorLogs_IsResolved] ON [ErrorLogs] ([IsResolved]);

CREATE INDEX [IX_ErrorLogs_LogLevel] ON [ErrorLogs] ([LogLevel]);

CREATE INDEX [IX_ErrorLogs_ModuleName] ON [ErrorLogs] ([ModuleName]);

CREATE INDEX [IX_ErrorLogs_RequestId] ON [ErrorLogs] ([RequestId]);

CREATE INDEX [IX_ErrorLogs_UserId] ON [ErrorLogs] ([UserId]);

CREATE UNIQUE INDEX [IX_EsslAttendanceSyncStates_Tenant] ON [EsslAttendanceSyncStates] ([TenantId]) WHERE [TenantId] IS NOT NULL;

CREATE UNIQUE INDEX [IX_EsslIntegrationSettings_Tenant] ON [EsslIntegrationSettings] ([TenantId]) WHERE [TenantId] IS NOT NULL;

CREATE INDEX [IX_EventParticipants_EmployeeId] ON [EventParticipants] ([EmployeeId]);

CREATE UNIQUE INDEX [IX_EventParticipants_EventId_EmployeeId] ON [EventParticipants] ([EventId], [EmployeeId]);

CREATE INDEX [IX_Events_DepartmentId] ON [Events] ([DepartmentId]);

CREATE INDEX [IX_Events_RoleId] ON [Events] ([RoleId]);

CREATE INDEX [IX_Events_StartDate_EndDate] ON [Events] ([StartDate], [EndDate]);

CREATE INDEX [IX_FinancialYears_CompanyId] ON [FinancialYears] ([CompanyId]);

CREATE INDEX [IX_FinancialYears_TenantId] ON [FinancialYears] ([TenantId]);

CREATE INDEX [IX_HolidayGroupDetails_HolidayGroupId] ON [HolidayGroupDetails] ([HolidayGroupId]);

CREATE INDEX [IX_InterviewSchedules_CandidateApplicationId] ON [InterviewSchedules] ([CandidateApplicationId]);

CREATE INDEX [IX_InterviewSchedules_InterviewerId] ON [InterviewSchedules] ([InterviewerId]);

CREATE INDEX [IX_JobOpenings_DepartmentId] ON [JobOpenings] ([DepartmentId]);

CREATE INDEX [IX_JobOpenings_DesignationId] ON [JobOpenings] ([DesignationId]);

CREATE INDEX [IX_LeaveApplications_EmployeeId] ON [LeaveApplications] ([EmployeeId]);

CREATE INDEX [IX_LeaveApplications_LeaveTypeId] ON [LeaveApplications] ([LeaveTypeId]);

CREATE INDEX [IX_LeaveApprovalHistories_LeaveApplicationId] ON [LeaveApprovalHistories] ([LeaveApplicationId]);

CREATE INDEX [IX_LeaveBalances_EmployeeId] ON [LeaveBalances] ([EmployeeId]);

CREATE INDEX [IX_LeaveBalances_LeaveTypeId] ON [LeaveBalances] ([LeaveTypeId]);

CREATE INDEX [IX_LeaveBalanceTransactions_EmployeeId] ON [LeaveBalanceTransactions] ([EmployeeId]);

CREATE INDEX [IX_LeaveBalanceTransactions_LeaveTypeId] ON [LeaveBalanceTransactions] ([LeaveTypeId]);

CREATE INDEX [IX_LoanAdvanceAttachments_Entity] ON [LoanAdvanceAttachments] ([EntityType], [EntityId]);

CREATE INDEX [IX_LoanAdvanceAttachments_UploadedByUserId] ON [LoanAdvanceAttachments] ([UploadedByUserId]);

CREATE INDEX [IX_LoanAdvanceAuditLogs_Entity] ON [LoanAdvanceAuditLogs] ([EntityType], [EntityId], [PerformedOn]);

CREATE INDEX [IX_LoanAdvanceAuditLogs_PerformedByUserId] ON [LoanAdvanceAuditLogs] ([PerformedByUserId]);

CREATE INDEX [IX_LoanAdvanceAuditLogs_TenantId] ON [LoanAdvanceAuditLogs] ([TenantId]);

CREATE INDEX [IX_LoanApprovalHistories_ActedAsDelegateForUserId] ON [LoanApprovalHistories] ([ActedAsDelegateForUserId]);

CREATE INDEX [IX_LoanApprovalHistories_CheckerId] ON [LoanApprovalHistories] ([CheckerId]);

CREATE INDEX [IX_LoanApprovalHistories_Loan] ON [LoanApprovalHistories] ([EmployeeLoanId], [LevelNumber]);

CREATE INDEX [IX_LoanEmiSchedules_DueTracking] ON [LoanEmiSchedules] ([Status], [DueDate]);

CREATE INDEX [IX_LoanEmiSchedules_PayrollId] ON [LoanEmiSchedules] ([PayrollId]);

CREATE UNIQUE INDEX [UX_LoanEmiSchedules] ON [LoanEmiSchedules] ([EmployeeLoanId], [InstallmentNumber]);

CREATE INDEX [IX_LoanPaymentHistories_Loan_Date] ON [LoanPaymentHistories] ([EmployeeLoanId], [PaymentDate]);

CREATE INDEX [IX_LoanPaymentHistories_LoanEmiScheduleId] ON [LoanPaymentHistories] ([LoanEmiScheduleId]);

CREATE INDEX [IX_LoanPaymentHistories_PaymentDate] ON [LoanPaymentHistories] ([PaymentDate]);

CREATE INDEX [IX_LoanPaymentHistories_PayrollId] ON [LoanPaymentHistories] ([PayrollId]);

CREATE INDEX [IX_LoanPolicies_BranchId] ON [LoanPolicies] ([BranchId]);

CREATE INDEX [IX_LoanPolicies_CompanyId] ON [LoanPolicies] ([CompanyId]);

CREATE INDEX [IX_LoanPolicies_LoanTypeId] ON [LoanPolicies] ([LoanTypeId]);

CREATE INDEX [IX_LoanPolicies_TenantId] ON [LoanPolicies] ([TenantId]);

CREATE INDEX [IX_LoanPolicyApprovalLevels_ApproverRoleId] ON [LoanPolicyApprovalLevels] ([ApproverRoleId]);

CREATE INDEX [IX_LoanPolicyApprovalLevels_ApproverUserId] ON [LoanPolicyApprovalLevels] ([ApproverUserId]);

CREATE UNIQUE INDEX [UX_LoanPolicyApprovalLevels] ON [LoanPolicyApprovalLevels] ([LoanPolicyId], [LevelNumber]);

CREATE UNIQUE INDEX [UX_LoanTypes_Tenant_Code] ON [LoanTypes] ([TenantId], [Code]) WHERE [IsDeleted] = 0;

CREATE INDEX [IX_Locations_BranchId] ON [Locations] ([BranchId]);

CREATE INDEX [IX_LoginHistories_TenantId] ON [LoginHistories] ([TenantId]);

CREATE INDEX [IX_LoginHistories_UserId] ON [LoginHistories] ([UserId]);

CREATE INDEX [IX_NotificationGroupAccesses_NotificationGroupId] ON [NotificationGroupAccesses] ([NotificationGroupId]);

CREATE INDEX [IX_NotificationGroupUsers_NotificationGroupId] ON [NotificationGroupUsers] ([NotificationGroupId]);

CREATE INDEX [IX_NotificationGroupUsers_UserId] ON [NotificationGroupUsers] ([UserId]);

CREATE INDEX [IX_NotificationRecipients_NotificationId] ON [NotificationRecipients] ([NotificationId]);

CREATE INDEX [IX_NotificationRecipients_UserId] ON [NotificationRecipients] ([UserId]);

CREATE INDEX [IX_Notifications_Reference_CreatedOn] ON [Notifications] ([ReferenceId], [CreatedOn]);

CREATE INDEX [IX_OnboardingCases_CandidateId] ON [OnboardingCases] ([CandidateId]);

CREATE INDEX [IX_OnboardingCases_EmployeeId] ON [OnboardingCases] ([EmployeeId]);

CREATE INDEX [IX_OnboardingCases_TenantId] ON [OnboardingCases] ([TenantId]);

CREATE INDEX [IX_OnboardingChecklistItems_OnboardingCaseId_StageType] ON [OnboardingChecklistItems] ([OnboardingCaseId], [StageType]);

CREATE INDEX [IX_OnboardingChecklistTemplateItems_TenantId_StageType] ON [OnboardingChecklistTemplateItems] ([TenantId], [StageType]);

CREATE INDEX [IX_OnDutyRequests_EmployeeId] ON [OnDutyRequests] ([EmployeeId]);

CREATE INDEX [IX_OnDutyRequests_TenantId_Status] ON [OnDutyRequests] ([TenantId], [Status]);

CREATE INDEX [IX_PayrollAuditLogs_PayrollId] ON [PayrollAuditLogs] ([PayrollId]);

CREATE INDEX [IX_PayrollDetails_PayrollId] ON [PayrollDetails] ([PayrollId]);

CREATE INDEX [IX_PayrollDetails_SalaryComponentId] ON [PayrollDetails] ([SalaryComponentId]);

CREATE INDEX [IX_Payrolls_EmployeeId_SalaryMonth] ON [Payrolls] ([EmployeeId], [SalaryMonth]);

CREATE INDEX [IX_PayslipRequestAudits_PayslipRequestId] ON [PayslipRequestAudits] ([PayslipRequestId]);

CREATE INDEX [IX_PayslipRequests_EmployeeId] ON [PayslipRequests] ([EmployeeId]);

CREATE INDEX [IX_PayslipRequests_PayrollId] ON [PayslipRequests] ([PayrollId]);

CREATE INDEX [IX_Payslips_PayrollId] ON [Payslips] ([PayrollId]);

CREATE INDEX [IX_PipRecords_EmployeeId] ON [PipRecords] ([EmployeeId]);

CREATE INDEX [IX_PipRecords_ProbationConfirmationId] ON [PipRecords] ([ProbationConfirmationId]);

CREATE INDEX [IX_PipRecords_TenantId_FinalOutcome] ON [PipRecords] ([TenantId], [FinalOutcome]);

CREATE INDEX [IX_ProbationConfirmations_EmployeeId] ON [ProbationConfirmations] ([EmployeeId]);

CREATE INDEX [IX_ProbationConfirmations_TenantId_Status] ON [ProbationConfirmations] ([TenantId], [Status]);

CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);

CREATE INDEX [IX_RejoiningHistories_EmployeeId] ON [RejoiningHistories] ([EmployeeId]);

CREATE INDEX [IX_RoleFeatures_AppFeatureId] ON [RoleFeatures] ([AppFeatureId]);

CREATE INDEX [IX_RoleFeatures_RoleId] ON [RoleFeatures] ([RoleId]);

CREATE INDEX [IX_RolePermissions_PermissionId] ON [RolePermissions] ([PermissionId]);

CREATE INDEX [IX_RolePermissions_RoleId] ON [RolePermissions] ([RoleId]);

CREATE INDEX [IX_SalaryDetails_SalaryComponentId] ON [SalaryDetails] ([SalaryComponentId]);

CREATE INDEX [IX_SalaryDetails_SalaryStructureId] ON [SalaryDetails] ([SalaryStructureId]);

CREATE INDEX [IX_SalaryStructures_EmployeeId_EffectiveFrom] ON [SalaryStructures] ([EmployeeId], [EffectiveFrom]);

CREATE INDEX [IX_SalaryStructures_SourceTemplateId] ON [SalaryStructures] ([SourceTemplateId]);

CREATE INDEX [IX_SalaryTemplateDetails_SalaryComponentId] ON [SalaryTemplateDetails] ([SalaryComponentId]);

CREATE INDEX [IX_SalaryTemplateDetails_SalaryTemplateId] ON [SalaryTemplateDetails] ([SalaryTemplateId]);

CREATE INDEX [IX_SalaryTemplates_Name] ON [SalaryTemplates] ([Name]);

CREATE INDEX [IX_Shifts_TenantId] ON [Shifts] ([TenantId]);

CREATE INDEX [IX_ShortLeaveRequests_EmployeeId] ON [ShortLeaveRequests] ([EmployeeId]);

CREATE INDEX [IX_ShortLeaveRequests_LeaveTypeId] ON [ShortLeaveRequests] ([LeaveTypeId]);

CREATE INDEX [IX_ShortLeaveRequests_TenantId_Status] ON [ShortLeaveRequests] ([TenantId], [Status]);

CREATE INDEX [IX_States_CountryId] ON [States] ([CountryId]);

CREATE INDEX [IX_SupportTicketReplies_SupportTicketId] ON [SupportTicketReplies] ([SupportTicketId]);

CREATE INDEX [IX_SupportTickets_EmployeeId] ON [SupportTickets] ([EmployeeId]);

CREATE UNIQUE INDEX [IX_TaxDeclarations_EmployeeId_FinancialYearId] ON [TaxDeclarations] ([EmployeeId], [FinancialYearId]);

CREATE INDEX [IX_TaxDeclarations_FinancialYearId] ON [TaxDeclarations] ([FinancialYearId]);

CREATE INDEX [IX_TaxDeclarations_TenantId_Status] ON [TaxDeclarations] ([TenantId], [Status]);

CREATE INDEX [IX_TaxSlabs_FinancialYearId_Regime] ON [TaxSlabs] ([FinancialYearId], [Regime]);

CREATE INDEX [IX_TenantFeatures_AppFeatureId] ON [TenantFeatures] ([AppFeatureId]);

CREATE INDEX [IX_TenantFeatures_TenantId] ON [TenantFeatures] ([TenantId]);

CREATE INDEX [IX_Tenants_CityId] ON [Tenants] ([CityId]);

CREATE INDEX [IX_Tenants_CountryId] ON [Tenants] ([CountryId]);

CREATE INDEX [IX_Tenants_StateId] ON [Tenants] ([StateId]);

CREATE INDEX [IX_UserFavoriteMenus_AppFeatureId] ON [UserFavoriteMenus] ([AppFeatureId]);

CREATE INDEX [IX_UserFavoriteMenus_UserId] ON [UserFavoriteMenus] ([UserId]);

CREATE UNIQUE INDEX [IX_UserFavoriteMenus_UserId_AppFeatureId] ON [UserFavoriteMenus] ([UserId], [AppFeatureId]) WHERE [IsDeleted] = 0;

CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);

CREATE INDEX [IX_UserRoles_UserId] ON [UserRoles] ([UserId]);

CREATE INDEX [IX_Users_BranchId] ON [Users] ([BranchId]);

CREATE INDEX [IX_Users_CompanyId] ON [Users] ([CompanyId]);

CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]) WHERE [Email] IS NOT NULL;

CREATE INDEX [IX_Users_EmployeeId] ON [Users] ([EmployeeId]);

CREATE INDEX [IX_Users_TenantId] ON [Users] ([TenantId]);

CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);

CREATE INDEX [IX_WfhRequests_EmployeeId] ON [WfhRequests] ([EmployeeId]);

CREATE INDEX [IX_WfhRequests_TenantId_Status] ON [WfhRequests] ([TenantId], [Status]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260915053128_InitalMigration', N'9.0.0');

COMMIT;
GO

