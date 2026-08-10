-- =====================================================
-- BUCKET TABLE
-- =====================================================
-- User-scoped named group for referable workspace objects (contacts, mailboxes).
-- See product §5.5 — buckets place (membership); name-only (e.g. "XYZ Inc", "Family").
--
-- Business Rules:
-- - Scoped to userId; soft delete via isDeleted
-- - name unique per user among non-deleted rows (app compares case-insensitive)
-- - Many-to-many with referable objects via workspace.BucketMember
-- - Apply after dbo.User ([workspace] schema must already exist)
-- - All records include audit fields for tracking changes
-- =====================================================
GO

CREATE TABLE [workspace].[Bucket] (
    -- Primary key with auto-generated sequential UUID
    [id]                                     UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    [userId]                                 UNIQUEIDENTIFIER NOT NULL,                 -- Owner (FK to User)

    -- Data fields
    [name]                                   NVARCHAR(128) NOT NULL,                    -- Group name (unique per user when not deleted)
    [sortOrder]                              INT NOT NULL DEFAULT 0,                    -- List order in Workspace UI

    -- Status and lifecycle management
    [isActive]                               BIT DEFAULT 1,
    [isDeleted]                              BIT DEFAULT 0,

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

-- Unique name per user among non-deleted rows
CREATE UNIQUE INDEX [IX_Bucket_UserId_Name]
    ON [workspace].[Bucket] ([userId], [name])
    WHERE [isDeleted] = 0;
GO

-- Index for listing buckets in sort order
CREATE INDEX [IX_Bucket_UserId_SortOrder]
    ON [workspace].[Bucket] ([userId], [sortOrder])
    WHERE [isDeleted] = 0;
GO

-- =====================================================
-- SEED DATA (optional — uncomment on new databases)
-- =====================================================
-- No default rows; buckets are user-created in Workspace.
-- GO
