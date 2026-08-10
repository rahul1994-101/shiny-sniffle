-- =====================================================
-- TAG TABLE
-- =====================================================
-- User-scoped label for referable workspace objects (contacts, mailboxes).
-- See product §5.5 — tags describe (facets, roles, topics); AI uses name, not color.
--
-- Business Rules:
-- - Scoped to userId; soft delete via isDeleted
-- - name unique per user among non-deleted rows (app compares case-insensitive)
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
    [name]                                   NVARCHAR(64) NOT NULL,                     -- Display label (unique per user when not deleted)
    [color]                                  NVARCHAR(9) NULL,                          -- Optional UI color (e.g. #RRGGBB)
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
    CONSTRAINT [FK_Tag_User] FOREIGN KEY ([userId]) REFERENCES [dbo].[User] ([id])
);
GO

-- =====================================================
-- INDEXES FOR TAG TABLE
-- =====================================================

-- Unique name per user among non-deleted rows
CREATE UNIQUE INDEX [IX_Tag_UserId_Name]
    ON [workspace].[Tag] ([userId], [name])
    WHERE [isDeleted] = 0;
GO

-- Index for listing tags in sort order
CREATE INDEX [IX_Tag_UserId_SortOrder]
    ON [workspace].[Tag] ([userId], [sortOrder])
    WHERE [isDeleted] = 0;
GO

-- =====================================================
-- SEED DATA (optional — uncomment on new databases)
-- =====================================================
-- No default rows; tags are user-created in Workspace.
-- GO
