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

-- =====================================================
-- SEED DATA (optional — uncomment on new databases)
-- =====================================================
-- Well-known providers: [isSystem] = 1 (app blocks edit/delete).
-- Custom: [isSystem] = 0 — editable template; set IMAP/SMTP hosts before connecting accounts.
-- Run after CREATE TABLE and indexes. Idempotent by [slug].
-- GO

-- IF NOT EXISTS (SELECT 1 FROM [dbo].[EmailProvider] WHERE [slug] = N'gmail' AND [isDeleted] = 0)
-- BEGIN
--     INSERT INTO [dbo].[EmailProvider] (
--         [name], [slug], [imapHost], [imapPort], [imapUseSsl],
--         [smtpHost], [smtpPort], [smtpUseSsl], [setupHelpUrl], [sortOrder], [isSystem])
--     VALUES (
--         N'Gmail', N'gmail', N'imap.gmail.com', 993, 1,
--         N'smtp.gmail.com', 587, 1, N'https://support.google.com/mail/answer/185833', 10, 1);
-- END
-- GO

-- IF NOT EXISTS (SELECT 1 FROM [dbo].[EmailProvider] WHERE [slug] = N'outlook' AND [isDeleted] = 0)
-- BEGIN
--     INSERT INTO [dbo].[EmailProvider] (
--         [name], [slug], [imapHost], [imapPort], [imapUseSsl],
--         [smtpHost], [smtpPort], [smtpUseSsl], [setupHelpUrl], [sortOrder], [isSystem])
--     VALUES (
--         N'Outlook.com', N'outlook', N'imap-mail.outlook.com', 993, 1,
--         N'smtp-mail.outlook.com', 587, 1, N'https://support.microsoft.com/office/outlook', 20, 1);
-- END
-- GO

-- IF NOT EXISTS (SELECT 1 FROM [dbo].[EmailProvider] WHERE [slug] = N'yahoo' AND [isDeleted] = 0)
-- BEGIN
--     INSERT INTO [dbo].[EmailProvider] (
--         [name], [slug], [imapHost], [imapPort], [imapUseSsl],
--         [smtpHost], [smtpPort], [smtpUseSsl], [setupHelpUrl], [sortOrder], [isSystem])
--     VALUES (
--         N'Yahoo Mail', N'yahoo', N'imap.mail.yahoo.com', 993, 1,
--         N'smtp.mail.yahoo.com', 465, 1, N'https://help.yahoo.com/kb/SLN4075.html', 30, 1);
-- END
-- GO

-- IF NOT EXISTS (SELECT 1 FROM [dbo].[EmailProvider] WHERE [slug] = N'icloud' AND [isDeleted] = 0)
-- BEGIN
--     INSERT INTO [dbo].[EmailProvider] (
--         [name], [slug], [imapHost], [imapPort], [imapUseSsl],
--         [smtpHost], [smtpPort], [smtpUseSsl], [setupHelpUrl], [sortOrder], [isSystem])
--     VALUES (
--         N'iCloud Mail', N'icloud', N'imap.mail.me.com', 993, 1,
--         N'smtp.mail.me.com', 587, 1, N'https://support.apple.com/icloud', 40, 1);
-- END
-- GO

-- IF NOT EXISTS (SELECT 1 FROM [dbo].[EmailProvider] WHERE [slug] = N'zoho' AND [isDeleted] = 0)
-- BEGIN
--     INSERT INTO [dbo].[EmailProvider] (
--         [name], [slug], [imapHost], [imapPort], [imapUseSsl],
--         [smtpHost], [smtpPort], [smtpUseSsl], [setupHelpUrl], [sortOrder], [isSystem])
--     VALUES (
--         N'Zoho Mail', N'zoho', N'imap.zoho.com', 993, 1,
--         N'smtp.zoho.com', 587, 1, N'https://www.zoho.com/mail/help/imap-access.html', 50, 1);
-- END
-- GO

-- IF NOT EXISTS (SELECT 1 FROM [dbo].[EmailProvider] WHERE [slug] = N'fastmail' AND [isDeleted] = 0)
-- BEGIN
--     INSERT INTO [dbo].[EmailProvider] (
--         [name], [slug], [imapHost], [imapPort], [imapUseSsl],
--         [smtpHost], [smtpPort], [smtpUseSsl], [setupHelpUrl], [sortOrder], [isSystem])
--     VALUES (
--         N'Fastmail', N'fastmail', N'imap.fastmail.com', 993, 1,
--         N'smtp.fastmail.com', 465, 1, N'https://www.fastmail.com/help/technical/servernames.html', 60, 1);
-- END
-- GO

-- IF NOT EXISTS (SELECT 1 FROM [dbo].[EmailProvider] WHERE [slug] = N'custom' AND [isDeleted] = 0)
-- BEGIN
--     INSERT INTO [dbo].[EmailProvider] (
--         [name], [slug], [imapHost], [imapPort], [imapUseSsl],
--         [smtpHost], [smtpPort], [smtpUseSsl], [setupHelpUrl], [sortOrder], [isSystem])
--     VALUES (
--         N'Custom', N'custom', N'', 993, 1,
--         N'', 587, 1, NULL, 999, 0);
-- END
-- GO
