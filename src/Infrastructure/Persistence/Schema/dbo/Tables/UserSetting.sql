-- =====================================================
-- USER SETTING TABLE
-- =====================================================
-- Per-user app preferences (Settings UI). One active row per user.
-- Login identity lives in dbo.User; connected mail in workspace.EmailAccount;
-- Workspace module data (contacts, etc.) lives in workspace schema.
--
-- Business Rules:
-- - Each user has at most one active settings row
-- - Add preference columns here as the product grows (timezone, defaults, UI prefs)
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

-- =====================================================
-- SEED DATA (optional — uncomment on new databases)
-- =====================================================
-- No default rows; one row per user when prefs are added in app.
-- GO
