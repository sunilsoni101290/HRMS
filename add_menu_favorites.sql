-- Menu Bar redesign - Favorites / Quick Access.
-- New UserFavoriteMenus table: one row per (UserId, AppFeatureId) pin, so a
-- user with 100+ AppFeature menu items can reach the handful they actually
-- use every day without walking the full sidebar tree. Pinning here is a
-- pure UI preference - it grants no access; GetMenuByUserAsync's existing
-- role/permission filtering still decides what a user may see, and the
-- Favorites list is always intersected against that (see
-- AppFeatureService.GetFavoritesAsync).
--
-- Idempotent - safe to re-run. Mirrors this repo's existing
-- "add Salary Processing revision (attendance-based proration).sql" style
-- (IF NOT EXISTS-guarded CREATE TABLE + indexes). If you have the .NET SDK
-- available, `dotnet ef migrations add AddUserFavoriteMenus --project
-- Infrastructure --startup-project API` should produce an equivalent
-- migration from the entity already added at
-- Domain/Entities/UserFavoriteMenu.cs - this script is the ready-to-run
-- equivalent for environments without the SDK on hand (see
-- Infrastructure/Migrations/20260905193440_AddUserFavoriteMenus.cs for the
-- migration-history-tracked copy of this same statement).

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserFavoriteMenus')
BEGIN
    CREATE TABLE [dbo].[UserFavoriteMenus] (
        [Id]            NVARCHAR(450)  NOT NULL,
        [TenantId]      NVARCHAR(MAX)  NULL,
        [UserId]        NVARCHAR(450)  NOT NULL,
        [AppFeatureId]  NVARCHAR(450)  NOT NULL,
        [DisplayOrder]  INT            NOT NULL CONSTRAINT DF_UserFavoriteMenus_DisplayOrder DEFAULT (0),
        [IsDeleted]     BIT            NOT NULL CONSTRAINT DF_UserFavoriteMenus_IsDeleted DEFAULT (0),
        [CreatedOn]     DATETIME2      NOT NULL CONSTRAINT DF_UserFavoriteMenus_CreatedOn DEFAULT (SYSUTCDATETIME()),
        [CreatedBy]     NVARCHAR(MAX)  NOT NULL,
        [ModifiedOn]    DATETIME2      NULL,
        [ModifiedBy]    NVARCHAR(MAX)  NULL,
        [IsActive]      BIT            NOT NULL CONSTRAINT DF_UserFavoriteMenus_IsActive DEFAULT (1),
        CONSTRAINT [PK_UserFavoriteMenus] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserFavoriteMenus_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserFavoriteMenus_AppFeatures_AppFeatureId] FOREIGN KEY ([AppFeatureId]) REFERENCES [dbo].[AppFeatures]([Id]) ON DELETE NO ACTION
    );
END

-- A user can pin the same AppFeature only once. Filtered so a soft-deleted
-- (un-pinned) row never blocks re-pinning the same item later.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserFavoriteMenus_UserId_AppFeatureId' AND object_id = OBJECT_ID('dbo.UserFavoriteMenus'))
    CREATE UNIQUE INDEX [IX_UserFavoriteMenus_UserId_AppFeatureId] ON [dbo].[UserFavoriteMenus] ([UserId], [AppFeatureId]) WHERE [IsDeleted] = 0;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserFavoriteMenus_UserId' AND object_id = OBJECT_ID('dbo.UserFavoriteMenus'))
    CREATE INDEX [IX_UserFavoriteMenus_UserId] ON [dbo].[UserFavoriteMenus] ([UserId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserFavoriteMenus_AppFeatureId' AND object_id = OBJECT_ID('dbo.UserFavoriteMenus'))
    CREATE INDEX [IX_UserFavoriteMenus_AppFeatureId] ON [dbo].[UserFavoriteMenus] ([AppFeatureId]);
