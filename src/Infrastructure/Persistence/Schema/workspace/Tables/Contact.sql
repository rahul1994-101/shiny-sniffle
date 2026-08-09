-- =====================================================
-- CONTACT TABLE
-- =====================================================
-- User-owned reference person for workflows and product features.
-- Not app login (dbo.User) and not a personal phonebook card.
--
-- Business Rules:
-- - Scoped to userId; soft delete via isDeleted
-- - alias NOT NULL; unique per user among non-deleted rows; optional in UI (auto-generated from name when blank)
-- - email and phone optional (use-case oriented)
-- - email unique per user among non-deleted rows when set
-- - source (ContactSource): system-set provenance — manual, import, from-email, agent, api; default 0
-- - Apply after dbo.User; run workspace/CreateSchema.sql first on new databases
-- - All records include audit fields for tracking changes
-- =====================================================
GO

CREATE TABLE [workspace].[Contact] (
    -- Primary key with auto-generated sequential UUID
    [id]                                     UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    [userId]                                 UNIQUEIDENTIFIER NOT NULL,                 -- Owner (FK to User)

    -- Data fields
    [firstName]                              NVARCHAR(50) NOT NULL,                     -- Given name; mail greetings (Hi {firstName})
    [lastName]                               NVARCHAR(50) NOT NULL DEFAULT N'',         -- Family name; optional formal salutations
    [alias]                                  NVARCHAR(64) NOT NULL,                     -- Per-user handle; app fills when user leaves blank
    [email]                                  NVARCHAR(255) NULL,                        -- Optional; unique per user when set
    [phone]                                  NVARCHAR(32) NULL,                         -- Optional contact phone
    [notes]                                  NVARCHAR(2000) NULL,                       -- Optional free-form context
    [source]                                 TINYINT NOT NULL DEFAULT 0,                -- ContactSource enum; app-set on create (not user-edited)
    [sortOrder]                              INT NOT NULL DEFAULT 0,                    -- List order in Workspace UI

    -- Status and lifecycle management
    [isActive]                               BIT DEFAULT 1,                            -- Whether the contact row is active
    [isDeleted]                              BIT DEFAULT 0,                            -- Soft delete flag for data retention

    -- Audit fields for tracking changes
    [createdBy]                              UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [createdAt]                              DATETIME2 DEFAULT SYSUTCDATETIME(),
    [updatedBy]                              UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [updatedAt]                              DATETIME2 DEFAULT SYSUTCDATETIME(),

    -- Foreign keys
    CONSTRAINT [FK_Contact_User] FOREIGN KEY ([userId]) REFERENCES [dbo].[User] ([id])
);
GO

-- =====================================================
-- INDEXES FOR CONTACT TABLE
-- =====================================================

-- Unique email per user among non-deleted rows when email is set
CREATE UNIQUE INDEX [IX_Contact_UserId_Email]
    ON [workspace].[Contact] ([userId], [email])
    WHERE [isDeleted] = 0 AND [email] IS NOT NULL;
GO

-- Unique alias per user among non-deleted rows
CREATE UNIQUE INDEX [IX_Contact_UserId_Alias]
    ON [workspace].[Contact] ([userId], [alias])
    WHERE [isDeleted] = 0;
GO

-- Index for listing contacts in sort order
CREATE INDEX [IX_Contact_UserId_SortOrder]
    ON [workspace].[Contact] ([userId], [sortOrder])
    WHERE [isDeleted] = 0;
GO

-- Index for filtering by active status
-- CREATE INDEX [IX_Contact_IsActive] ON [workspace].[Contact] ([isActive]) WHERE [isActive] = 1;
-- GO

-- Index for filtering by deletion status
-- CREATE INDEX [IX_Contact_IsDeleted] ON [workspace].[Contact] ([isDeleted]) WHERE [isDeleted] = 0;
-- GO

-- Composite index for active, non-deleted records
-- CREATE INDEX [IX_Contact_IsActive_IsDeleted] ON [workspace].[Contact] ([isActive], [isDeleted]) WHERE [isActive] = 1 AND [isDeleted] = 0;
-- GO

-- Index for audit queries
-- CREATE INDEX [IX_Contact_CreatedAt] ON [workspace].[Contact] ([createdAt] DESC);
-- GO

-- Index for finding records by creator
-- CREATE INDEX [IX_Contact_CreatedBy] ON [workspace].[Contact] ([createdBy]);
-- GO

-- Index for finding records by updater
-- CREATE INDEX [IX_Contact_UpdatedBy] ON [workspace].[Contact] ([updatedBy]);
-- GO

-- Index for finding records by last update time
-- CREATE INDEX [IX_Contact_UpdatedAt] ON [workspace].[Contact] ([updatedAt] DESC);
-- GO

-- =====================================================
-- SEED DATA (optional — uncomment on new databases)
-- =====================================================
-- No default rows; contacts are user-created in Workspace.
-- GO

-- =====================================================
-- EXISTING DATABASE (optional — alias NOT NULL)
-- =====================================================
-- IF EXISTS (SELECT 1 FROM [workspace].[Contact] WHERE [alias] IS NULL)
-- BEGIN
--     UPDATE c SET [alias] = LEFT(LOWER(REPLACE(LTRIM(RTRIM([firstName])), N' ', N'-')), 64)
--     FROM [workspace].[Contact] c WHERE c.[alias] IS NULL AND LTRIM(RTRIM(c.[firstName])) <> N'';
--     UPDATE [workspace].[Contact] SET [alias] = N'contact' WHERE [alias] IS NULL;
-- END
-- IF COL_LENGTH(N'workspace.Contact', N'alias') IS NOT NULL
--     ALTER TABLE [workspace].[Contact] ALTER COLUMN [alias] NVARCHAR(64) NOT NULL;
-- GO
