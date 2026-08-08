-- =====================================================
-- USER SETTING TABLE
-- =====================================================
-- Per-user workspace settings (one active row per user).
-- Connected mail credentials live in dbo.EmailAccount.
--
-- Business Rules:
-- - Each user has at most one active settings row
-- - Additional settings areas can be added as columns later
-- - All records include audit fields for tracking changes
-- =====================================================
GO

CREATE TABLE [dbo].[UserSetting] (
    -- Primary key with auto-generated sequential UUID
    [id]                                     UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    [userId]                                 UNIQUEIDENTIFIER NOT NULL,                 -- Owner of the settings row (FK to User)

    -- Status and lifecycle management
    [isActive]                               BIT DEFAULT 1,                            -- Whether the settings row is active
    [isDeleted]                              BIT DEFAULT 0,                            -- Soft delete flag for data retention

    -- Audit fields for tracking changes
    [createdBy]                              UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [createdAt]                              DATETIME2 DEFAULT SYSUTCDATETIME(),
    [updatedBy]                              UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [updatedAt]                              DATETIME2 DEFAULT SYSUTCDATETIME(),

    -- Foreign keys
    CONSTRAINT [FK_UserSetting_User] FOREIGN KEY ([userId]) REFERENCES [dbo].[User] ([id])
);
GO

-- =====================================================
-- INDEXES FOR USER SETTING TABLE
-- =====================================================

-- Unique index: one active settings row per user
CREATE UNIQUE INDEX [IX_UserSetting_UserId] ON [dbo].[UserSetting] ([userId]) WHERE [isDeleted] = 0;
GO
