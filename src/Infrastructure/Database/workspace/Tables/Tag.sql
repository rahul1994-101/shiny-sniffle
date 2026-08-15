-- =====================================================
-- TAG TABLE
-- =====================================================
-- User-owned referable label (Workspace → Tags; tag:{alias}).
-- Describes contacts and mailboxes via TagAssignment — see product §5.5.
--
-- Business Rules:
-- - Scoped to userId; soft delete via isDeleted
-- - alias + context: see Agent reference section (alias NOT NULL; optional in UI with auto-generate)
-- - name is display-only (not unique); alias is the per-user handle for AI refs (tag:{alias})
-- - color is UI-only (optional hex)
-- - Apply after dbo.User ([workspace] schema must already exist)
-- - All records include audit fields for tracking changes
-- =====================================================
GO

CREATE TABLE [workspace].[Tag] (
    -- Primary key with auto-generated sequential UUID
    [id]                                     UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    [userId]                                 UNIQUEIDENTIFIER NOT NULL,                 -- Owner (FK to User)

    -- Data fields
    [name]                                   NVARCHAR(64) NOT NULL,                     -- Display label (not unique)
    [color]                                  NVARCHAR(9) NULL,                          -- Optional UI color (e.g. #RRGGBB)
    [sortOrder]                              INT NOT NULL DEFAULT 0,                    -- List order in Workspace UI

    -- Agent reference (alias + context for tools and prompts)
    [alias]                                  NVARCHAR(64) NOT NULL,                     -- Per-user handle (NOT NULL); optional in UI; auto-generated from name when blank
    [context]                                NVARCHAR(2000) NULL,                       -- Optional facts for UI, rules, and agent prompts

    -- Status and lifecycle management
    [isActive]                               BIT DEFAULT 1,                             -- Whether the tag row is active
    [isDeleted]                              BIT DEFAULT 0,                             -- Soft delete flag for data retention

    -- Audit fields for tracking changes
    [createdBy]                              UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [createdAt]                              DATETIME2 DEFAULT SYSUTCDATETIME(),
    [updatedBy]                              UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [updatedAt]                              DATETIME2 DEFAULT SYSUTCDATETIME(),

    -- Foreign keys
    CONSTRAINT [FK_Tag_User] FOREIGN KEY ([userId]) REFERENCES [dbo].[User] ([id])
);
GO

-- =====================================================
-- INDEXES FOR TAG TABLE
-- =====================================================

-- Unique alias per user among non-deleted rows (AI ref: tag:{alias})
CREATE UNIQUE INDEX [IX_Tag_UserId_Alias] ON [workspace].[Tag] ([userId], [alias]) WHERE [isDeleted] = 0;
GO

-- Index for listing tags in sort order
CREATE INDEX [IX_Tag_UserId_SortOrder] ON [workspace].[Tag] ([userId], [sortOrder]) WHERE [isDeleted] = 0;
GO

-- Index for filtering by active status
-- CREATE INDEX [IX_Tag_IsActive] ON [workspace].[Tag] ([isActive]) WHERE [isActive] = 1;
-- GO

-- Index for filtering by deletion status
-- CREATE INDEX [IX_Tag_IsDeleted] ON [workspace].[Tag] ([isDeleted]) WHERE [isDeleted] = 0;
-- GO

-- Composite index for active, non-deleted records
-- CREATE INDEX [IX_Tag_IsActive_IsDeleted] ON [workspace].[Tag] ([isActive], [isDeleted]) WHERE [isActive] = 1 AND [isDeleted] = 0;
-- GO

-- Index for audit queries
-- CREATE INDEX [IX_Tag_CreatedAt] ON [workspace].[Tag] ([createdAt] DESC);
-- GO

-- Index for finding records by creator
-- CREATE INDEX [IX_Tag_CreatedBy] ON [workspace].[Tag] ([createdBy]);
-- GO

-- Index for finding records by updater
-- CREATE INDEX [IX_Tag_UpdatedBy] ON [workspace].[Tag] ([updatedBy]);
-- GO

-- Index for finding records by last update time
-- CREATE INDEX [IX_Tag_UpdatedAt] ON [workspace].[Tag] ([updatedAt] DESC);
-- GO

-- =====================================================
-- SEED DATA (optional — uncomment on new databases)
-- =====================================================
-- No default rows; tags are user-created in Workspace.
-- GO
