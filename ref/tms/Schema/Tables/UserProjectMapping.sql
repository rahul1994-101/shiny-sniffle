-- =====================================================
-- USER PROJECT MAPPING TABLE
-- =====================================================
-- This table links users to projects with their project-specific roles.
-- 
-- Business Rules:
-- - A user can have only one role per project
-- - Project roles stored as enum: 0 = Viewer, 1 = Contributor, 2 = ProjectAdmin
-- - All records include audit fields for tracking changes
-- =====================================================

CREATE TABLE [dbo].[UserProjectMapping] (
    -- Primary key with auto-generated sequential UUID
    [id]                                    UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),

    -- Foreign keys
    [userId]                                UNIQUEIDENTIFIER NOT NULL,                 -- Reference to User
    [projectId]                             UNIQUEIDENTIFIER NOT NULL,                 -- Reference to Project

    -- Data fields
    [role]                                  TINYINT NOT NULL DEFAULT 0,                -- Project role enum: 0 = Viewer, 1 = Contributor, 2 = ProjectAdmin

    -- Status and lifecycle management
    [isActive]                              BIT DEFAULT 1,                             -- Whether the mapping is active
    [isDeleted]                             BIT DEFAULT 0,                             -- Soft delete flag for data retention

    -- Audit fields for tracking changes
    [createdBy]                             UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [createdAt]                             DATETIME2 DEFAULT SYSUTCDATETIME(),
    [updatedBy]                             UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [updatedAt]                             DATETIME2 DEFAULT SYSUTCDATETIME(),

    -- Foreign key constraints
    CONSTRAINT [FK_UserProjectMapping_User]    FOREIGN KEY ([userId])    REFERENCES [dbo].[User]([id]),
    CONSTRAINT [FK_UserProjectMapping_Project] FOREIGN KEY ([projectId]) REFERENCES [dbo].[Project]([id])
);

-- =====================================================
-- INDEXES FOR USER PROJECT MAPPING TABLE
-- =====================================================

-- Unique index for user-project combination (enforces one role per user per project)
CREATE UNIQUE INDEX [IX_UserProjectMapping_User_Project] ON [dbo].[UserProjectMapping] ([userId], [projectId]) WHERE [isDeleted] = 0;

-- Index for filtering by user (find all projects for a user)
-- CREATE INDEX [IX_UserProjectMapping_UserId] ON [dbo].[UserProjectMapping] ([userId]) WHERE [isActive] = 1 AND [isDeleted] = 0;

-- Index for filtering by project (find all members of a project)
-- CREATE INDEX [IX_UserProjectMapping_ProjectId] ON [dbo].[UserProjectMapping] ([projectId]) WHERE [isActive] = 1 AND [isDeleted] = 0;

-- Index for role queries
--  INDEX [IX_UserProjectMapping_Role] ON [dbo].[UserProjectMapping] ([role]);

-- Index for filtering by active status
-- CREATE INDEX [IX_UserProjectMapping_IsActive] ON [dbo].[UserProjectMapping] ([isActive]) WHERE [isActive] = 1;

-- Index for filtering by deletion status
-- CREATE INDEX [IX_UserProjectMapping_IsDeleted] ON [dbo].[UserProjectMapping] ([isDeleted]) WHERE [isDeleted] = 0;

-- Composite index for active, non-deleted mappings
-- CREATE INDEX [IX_UserProjectMapping_IsActive_IsDeleted] ON [dbo].[UserProjectMapping] ([isActive], [isDeleted]) WHERE [isActive] = 1 AND [isDeleted] = 0;

-- Index for audit queries
-- CREATE INDEX [IX_UserProjectMapping_CreatedAt] ON [dbo].[UserProjectMapping] ([createdAt] DESC);

-- Index for finding records by creator/updater
-- CREATE INDEX [IX_UserProjectMapping_CreatedBy] ON [dbo].[UserProjectMapping] ([createdBy]);
