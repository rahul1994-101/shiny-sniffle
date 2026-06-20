-- =====================================================
-- USER SETTING TABLE
-- =====================================================
-- This table stores per-user workspace settings as JSON columns.
--
-- Business Rules:
-- - Each user has at most one active settings row
-- - Mailbox (IMAP/SMTP) credentials live in emailSettings as encrypted JSON
-- - Additional settings areas can be added as new JSON columns later
-- - All records include audit fields for tracking changes
-- =====================================================
GO

CREATE TABLE [dbo].[UserSetting] (
    -- Primary key with auto-generated sequential UUID
    [id]                                     UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    [userId]                                 UNIQUEIDENTIFIER NOT NULL,                 -- Owner of the settings row (FK to User)

    -- Data fields
    [emailSettings]                          NVARCHAR(MAX) NULL,                        -- Mailbox IMAP/SMTP settings (JSON; password encrypted)

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

-- Index for filtering by active status
-- CREATE INDEX [IX_UserSetting_IsActive] ON [dbo].[UserSetting] ([isActive]) WHERE [isActive] = 1;
-- GO

-- Index for filtering by deletion status
-- CREATE INDEX [IX_UserSetting_IsDeleted] ON [dbo].[UserSetting] ([isDeleted]) WHERE [isDeleted] = 0;
-- GO

-- Composite index for active, non-deleted records
-- CREATE INDEX [IX_UserSetting_IsActive_IsDeleted] ON [dbo].[UserSetting] ([isActive], [isDeleted]) WHERE [isActive] = 1 AND [isDeleted] = 0;
-- GO

-- Index for audit queries
-- CREATE INDEX [IX_UserSetting_CreatedAt] ON [dbo].[UserSetting] ([createdAt] DESC);
-- GO

-- Index for finding records by creator
-- CREATE INDEX [IX_UserSetting_CreatedBy] ON [dbo].[UserSetting] ([createdBy]);
-- GO

-- Index for finding records by updater
-- CREATE INDEX [IX_UserSetting_UpdatedBy] ON [dbo].[UserSetting] ([updatedBy]);
-- GO

-- Index for finding records by last update time
-- CREATE INDEX [IX_UserSetting_UpdatedAt] ON [dbo].[UserSetting] ([updatedAt] DESC);
-- GO
