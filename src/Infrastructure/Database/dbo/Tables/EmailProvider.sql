-- =====================================================
-- EMAIL PROVIDER TABLE
-- =====================================================
-- Catalog of IMAP/SMTP endpoint templates (Settings → Email providers).
--
-- Business Rules:
-- - System rows (isSystem = 1, userId NULL): global read-only templates for all users
-- - Custom rows (isSystem = 0, userId set): owned by one user; that user may add/update/delete
-- - Slug unique among system rows; slug unique per user among custom rows
-- - Does not store mailbox passwords; per-user credentials live in workspace.EmailAccount
-- - All records include audit fields for tracking changes
-- =====================================================
GO

CREATE TABLE [dbo].[EmailProvider] (
    -- Primary key with auto-generated sequential UUID
    [id]                                    UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),

    -- Ownership (NULL = system template)
    [userId]                                UNIQUEIDENTIFIER NULL,

    -- Data fields
    [name]                                  NVARCHAR(100) NOT NULL,                    -- Display name (e.g. Gmail)
    [slug]                                  NVARCHAR(64) NOT NULL,                     -- URL-safe key (e.g. gmail, my-work-mail)
    [imapHost]                              NVARCHAR(255) NOT NULL DEFAULT '',         -- IMAP server host
    [imapPort]                              INT NOT NULL DEFAULT 993 CHECK ([imapPort] BETWEEN 1 AND 65535), -- IMAP port
    [imapUseSsl]                            BIT DEFAULT 1,                             -- Use SSL/TLS for IMAP
    [smtpHost]                              NVARCHAR(255) NOT NULL DEFAULT '',         -- SMTP server host
    [smtpPort]                              INT NOT NULL DEFAULT 587 CHECK ([smtpPort] BETWEEN 1 AND 65535), -- SMTP port
    [smtpUseSsl]                            BIT DEFAULT 1,                             -- Use SSL/TLS for SMTP
    [setupHelpUrl]                          NVARCHAR(500) NULL,                        -- Optional link to provider setup docs
    [sortOrder]                             INT NOT NULL DEFAULT 0,                    -- List order in UI
    [isSystem]                              BIT DEFAULT 0,                             -- Seeded global template; app blocks edit/delete

    -- Status and lifecycle management
    [isActive]                               BIT DEFAULT 1,                            -- Whether the provider row is active
    [isDeleted]                              BIT DEFAULT 0,                            -- Soft delete flag for data retention

    -- Audit fields for tracking changes
    [createdBy]                              UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [createdAt]                              DATETIME2 DEFAULT SYSUTCDATETIME(),
    [updatedBy]                              UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [updatedAt]                              DATETIME2 DEFAULT SYSUTCDATETIME(),

    -- Foreign keys
    CONSTRAINT [FK_EmailProvider_User] FOREIGN KEY ([userId]) REFERENCES [dbo].[User] ([id]),

    -- System rows are global (userId NULL); custom rows are user-owned (userId required)
    CONSTRAINT [CK_EmailProvider_Ownership] CHECK (
        ([isSystem] = 1 AND [userId] IS NULL) OR ([isSystem] = 0 AND [userId] IS NOT NULL)
    ),

    -- Active providers must have non-empty server hosts
    CONSTRAINT [CK_EmailProvider_Hosts] CHECK (
        LEN(LTRIM(RTRIM([imapHost]))) > 0 AND LEN(LTRIM(RTRIM([smtpHost]))) > 0
    )
);
GO

-- =====================================================
-- INDEXES FOR EMAIL PROVIDER TABLE
-- =====================================================

-- Unique slug among system templates
CREATE UNIQUE INDEX [IX_EmailProvider_System_Slug] ON [dbo].[EmailProvider] ([slug]) WHERE [isSystem] = 1 AND [isDeleted] = 0;
GO

-- Unique slug per user among custom templates
CREATE UNIQUE INDEX [IX_EmailProvider_UserId_Slug] ON [dbo].[EmailProvider] ([userId], [slug]) WHERE [isSystem] = 0 AND [isDeleted] = 0;
GO

-- Index for listing providers in sort order
CREATE INDEX [IX_EmailProvider_SortOrder] ON [dbo].[EmailProvider] ([sortOrder]) WHERE [isDeleted] = 0;
GO

-- Index for listing a user's custom providers
CREATE INDEX [IX_EmailProvider_UserId] ON [dbo].[EmailProvider] ([userId]) WHERE [isSystem] = 0 AND [isDeleted] = 0;
GO

-- =====================================================
-- SEED DATA (optional — uncomment on new databases)
-- =====================================================
-- Well-known providers: [isSystem] = 1, [userId] = NULL (app blocks edit/delete).
-- Custom templates: created per user in app ([isSystem] = 0, [userId] = owner).
-- Run after CREATE TABLE and indexes. Idempotent by [slug] among system rows.
-- GO

-- IF NOT EXISTS (SELECT 1 FROM [dbo].[EmailProvider] WHERE [slug] = N'gmail' AND [isSystem] = 1 AND [isDeleted] = 0)
-- BEGIN
--     INSERT INTO [dbo].[EmailProvider] (
--         [name], [slug], [imapHost], [imapPort], [imapUseSsl],
--         [smtpHost], [smtpPort], [smtpUseSsl], [setupHelpUrl], [sortOrder], [isSystem])
--     VALUES (
--         N'Gmail', N'gmail', N'imap.gmail.com', 993, 1,
--         N'smtp.gmail.com', 587, 1, N'https://support.google.com/mail/answer/185833', 10, 1);
-- END
-- GO

-- IF NOT EXISTS (SELECT 1 FROM [dbo].[EmailProvider] WHERE [slug] = N'outlook' AND [isSystem] = 1 AND [isDeleted] = 0)
-- BEGIN
--     INSERT INTO [dbo].[EmailProvider] (
--         [name], [slug], [imapHost], [imapPort], [imapUseSsl],
--         [smtpHost], [smtpPort], [smtpUseSsl], [setupHelpUrl], [sortOrder], [isSystem])
--     VALUES (
--         N'Outlook.com', N'outlook', N'imap-mail.outlook.com', 993, 1,
--         N'smtp-mail.outlook.com', 587, 1, N'https://support.microsoft.com/office/outlook', 20, 1);
-- END
-- GO

-- IF NOT EXISTS (SELECT 1 FROM [dbo].[EmailProvider] WHERE [slug] = N'yahoo' AND [isSystem] = 1 AND [isDeleted] = 0)
-- BEGIN
--     INSERT INTO [dbo].[EmailProvider] (
--         [name], [slug], [imapHost], [imapPort], [imapUseSsl],
--         [smtpHost], [smtpPort], [smtpUseSsl], [setupHelpUrl], [sortOrder], [isSystem])
--     VALUES (
--         N'Yahoo Mail', N'yahoo', N'imap.mail.yahoo.com', 993, 1,
--         N'smtp.mail.yahoo.com', 465, 1, N'https://help.yahoo.com/kb/SLN4075.html', 30, 1);
-- END
-- GO

-- IF NOT EXISTS (SELECT 1 FROM [dbo].[EmailProvider] WHERE [slug] = N'icloud' AND [isSystem] = 1 AND [isDeleted] = 0)
-- BEGIN
--     INSERT INTO [dbo].[EmailProvider] (
--         [name], [slug], [imapHost], [imapPort], [imapUseSsl],
--         [smtpHost], [smtpPort], [smtpUseSsl], [setupHelpUrl], [sortOrder], [isSystem])
--     VALUES (
--         N'iCloud Mail', N'icloud', N'imap.mail.me.com', 993, 1,
--         N'smtp.mail.me.com', 587, 1, N'https://support.apple.com/icloud', 40, 1);
-- END
-- GO

-- IF NOT EXISTS (SELECT 1 FROM [dbo].[EmailProvider] WHERE [slug] = N'zoho' AND [isSystem] = 1 AND [isDeleted] = 0)
-- BEGIN
--     INSERT INTO [dbo].[EmailProvider] (
--         [name], [slug], [imapHost], [imapPort], [imapUseSsl],
--         [smtpHost], [smtpPort], [smtpUseSsl], [setupHelpUrl], [sortOrder], [isSystem])
--     VALUES (
--         N'Zoho Mail', N'zoho', N'imap.zoho.com', 993, 1,
--         N'smtp.zoho.com', 587, 1, N'https://www.zoho.com/mail/help/imap-access.html', 50, 1);
-- END
-- GO

-- IF NOT EXISTS (SELECT 1 FROM [dbo].[EmailProvider] WHERE [slug] = N'fastmail' AND [isSystem] = 1 AND [isDeleted] = 0)
-- BEGIN
--     INSERT INTO [dbo].[EmailProvider] (
--         [name], [slug], [imapHost], [imapPort], [imapUseSsl],
--         [smtpHost], [smtpPort], [smtpUseSsl], [setupHelpUrl], [sortOrder], [isSystem])
--     VALUES (
--         N'Fastmail', N'fastmail', N'imap.fastmail.com', 993, 1,
--         N'smtp.fastmail.com', 465, 1, N'https://www.fastmail.com/help/technical/servernames.html', 60, 1);
-- END
-- GO
