-- =====================================================
-- BUCKET TABLE
-- =====================================================
-- User-owned referable group (Workspace → Buckets; bucket:{alias}).
-- Groups contacts and mailboxes via BucketAssignment — see product §5.5.
--
-- Business Rules:
-- - Scoped to userId
-- - isDeleted: user removed the row — gone from UI; retained internally (soft delete)
-- - isActive: user paused the row — reversible; excluded from runtime until reactivated
-- - alias + context: see Agent reference section (alias NOT NULL; optional in UI with auto-generate)
-- - name is display-only (not unique); alias is the per-user handle for AI refs (bucket:{alias})
-- - color is UI-only (optional hex)
-- - Apply after dbo.User ([workspace] schema must already exist)
-- - All records include audit fields for tracking changes
-- =====================================================
GO

CREATE TABLE [workspace].[Bucket] (
    -- Primary key with auto-generated sequential UUID
    [id]                                     UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    [userId]                                 UNIQUEIDENTIFIER NOT NULL,                 -- Owner (FK to User)

    -- Data fields
    [name]                                   NVARCHAR(128) NOT NULL,                    -- Display label (not unique)
    [color]                                  NVARCHAR(9) NULL,                          -- Optional UI color (e.g. #RRGGBB)

    -- Agent reference (alias + context for tools and prompts)
    [alias]                                  NVARCHAR(64) NOT NULL,                     -- Per-user handle (NOT NULL); optional in UI; auto-generated from name when blank
    [context]                                NVARCHAR(2000) NULL,                       -- Optional facts for UI, rules, and agent prompts

    -- Status and lifecycle management
    [isActive]                               BIT DEFAULT 1,                             -- Paused by user; reversible (deactivate / reactivate)
    [isDeleted]                              BIT DEFAULT 0,                             -- Removed by user; hidden permanently; row retained

    -- Audit fields for tracking changes
    [createdBy]                              UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [createdAt]                              DATETIME2 DEFAULT SYSUTCDATETIME(),
    [updatedBy]                              UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [updatedAt]                              DATETIME2 DEFAULT SYSUTCDATETIME(),

    -- Foreign keys
    CONSTRAINT [FK_Bucket_User] FOREIGN KEY ([userId]) REFERENCES [dbo].[User] ([id])
);
GO

-- =====================================================
-- INDEXES FOR BUCKET TABLE
-- =====================================================

-- Unique alias per user among non-deleted rows (AI ref: bucket:{alias})
CREATE UNIQUE INDEX [IX_Bucket_UserId_Alias] ON [workspace].[Bucket] ([userId], [alias]) WHERE [isDeleted] = 0;
GO

-- Index for filtering by active status
-- CREATE INDEX [IX_Bucket_IsActive] ON [workspace].[Bucket] ([isActive]) WHERE [isActive] = 1;
-- GO

-- Index for filtering by deletion status
-- CREATE INDEX [IX_Bucket_IsDeleted] ON [workspace].[Bucket] ([isDeleted]) WHERE [isDeleted] = 0;
-- GO

-- Composite index for active, non-deleted records
-- CREATE INDEX [IX_Bucket_IsActive_IsDeleted] ON [workspace].[Bucket] ([isActive], [isDeleted]) WHERE [isActive] = 1 AND [isDeleted] = 0;
-- GO

-- Index for audit queries
-- CREATE INDEX [IX_Bucket_CreatedAt] ON [workspace].[Bucket] ([createdAt] DESC);
-- GO

-- Index for finding records by creator
-- CREATE INDEX [IX_Bucket_CreatedBy] ON [workspace].[Bucket] ([createdBy]);
-- GO

-- Index for finding records by updater
-- CREATE INDEX [IX_Bucket_UpdatedBy] ON [workspace].[Bucket] ([updatedBy]);
-- GO

-- Index for finding records by last update time
-- CREATE INDEX [IX_Bucket_UpdatedAt] ON [workspace].[Bucket] ([updatedAt] DESC);
-- GO

-- =====================================================
-- SEED DATA (optional — uncomment on new databases)
-- =====================================================
-- No default rows; buckets are user-created in Workspace.
-- GO
