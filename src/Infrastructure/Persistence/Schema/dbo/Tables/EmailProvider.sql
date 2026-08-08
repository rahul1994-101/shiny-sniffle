-- =====================================================
-- EMAIL PROVIDER TABLE
-- =====================================================
-- Catalog of IMAP/SMTP endpoint templates (Settings → Email providers).
--
-- Business Rules:
-- - Each provider has a unique slug among non-deleted rows
-- - System rows (isSystem = 1) are seeded and cannot be deleted in app
-- - Does not store mailbox passwords; per-user credentials live in dbo.EmailAccount
-- - All records include audit fields for tracking changes
-- =====================================================
GO

CREATE TABLE [dbo].[EmailProvider] (
    -- Primary key with auto-generated sequential UUID
    [id]                                    UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),

    -- Data fields
    [name]                                  NVARCHAR(100) NOT NULL,                    -- Display name (e.g. Gmail)
    [slug]                                  NVARCHAR(64) NOT NULL,                     -- URL-safe key (e.g. gmail, custom)
    [imapHost]                              NVARCHAR(255) NOT NULL DEFAULT '',         -- IMAP server host
    [imapPort]                              INT NOT NULL DEFAULT 993,                  -- IMAP port
    [imapUseSsl]                            BIT DEFAULT 1,                             -- Use SSL/TLS for IMAP
    [smtpHost]                              NVARCHAR(255) NOT NULL DEFAULT '',         -- SMTP server host
    [smtpPort]                              INT NOT NULL DEFAULT 587,                  -- SMTP port
    [smtpUseSsl]                            BIT DEFAULT 1,                             -- Use SSL/TLS for SMTP
    [setupHelpUrl]                          NVARCHAR(500) NULL,                        -- Optional link to provider setup docs
    [sortOrder]                             INT NOT NULL DEFAULT 0,                    -- List order in UI
    [isSystem]                              BIT DEFAULT 0,                             -- Seeded catalog row; app blocks delete

    -- Status and lifecycle management
    [isActive]                               BIT DEFAULT 1,                            -- Whether the provider row is active
    [isDeleted]                              BIT DEFAULT 0,                            -- Soft delete flag for data retention

    -- Audit fields for tracking changes
    [createdBy]                              UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [createdAt]                              DATETIME2 DEFAULT SYSUTCDATETIME(),
    [updatedBy]                              UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [updatedAt]                              DATETIME2 DEFAULT SYSUTCDATETIME()
);
GO

-- =====================================================
-- INDEXES FOR EMAIL PROVIDER TABLE
-- =====================================================

-- Unique slug among active catalog rows
CREATE UNIQUE INDEX [IX_EmailProvider_Slug] ON [dbo].[EmailProvider] ([slug]) WHERE [isDeleted] = 0;
GO

-- Index for listing providers in sort order
CREATE INDEX [IX_EmailProvider_SortOrder] ON [dbo].[EmailProvider] ([sortOrder]) WHERE [isDeleted] = 0;
GO

-- Index for filtering by active status
-- CREATE INDEX [IX_EmailProvider_IsActive] ON [dbo].[EmailProvider] ([isActive]) WHERE [isActive] = 1;
-- GO

-- Index for filtering by deletion status
-- CREATE INDEX [IX_EmailProvider_IsDeleted] ON [dbo].[EmailProvider] ([isDeleted]) WHERE [isDeleted] = 0;
-- GO

-- Composite index for active, non-deleted records
-- CREATE INDEX [IX_EmailProvider_IsActive_IsDeleted] ON [dbo].[EmailProvider] ([isActive], [isDeleted]) WHERE [isActive] = 1 AND [isDeleted] = 0;
-- GO

-- Index for audit queries
-- CREATE INDEX [IX_EmailProvider_CreatedAt] ON [dbo].[EmailProvider] ([createdAt] DESC);
-- GO

-- Index for finding records by creator
-- CREATE INDEX [IX_EmailProvider_CreatedBy] ON [dbo].[EmailProvider] ([createdBy]);
-- GO

-- Index for finding records by updater
-- CREATE INDEX [IX_EmailProvider_UpdatedBy] ON [dbo].[EmailProvider] ([updatedBy]);
-- GO

-- Index for finding records by last update time
-- CREATE INDEX [IX_EmailProvider_UpdatedAt] ON [dbo].[EmailProvider] ([updatedAt] DESC);
-- GO
