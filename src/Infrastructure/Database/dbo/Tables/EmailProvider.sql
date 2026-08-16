-- =====================================================
-- EMAIL PROVIDER TABLE
-- =====================================================
-- Catalog of IMAP/SMTP endpoint templates (Settings → Email providers).
--
-- Business Rules:
-- - System rows (isSystem = 1, userId NULL): global read-only templates for all users
-- - Custom rows (isSystem = 0, userId set): owned by one user; that user may add/update/delete
-- - Identified by [id]; [name] is display-only for pickers
-- - Does not store mailbox passwords; per-user credentials live in workspace.EmailAccount
-- - isDeleted: user removed the row — gone from UI; retained internally (soft delete)
-- - isActive: user paused the row — reversible; excluded from runtime until reactivated
-- - All records include audit fields for tracking changes
-- =====================================================
GO

CREATE TABLE [dbo].[EmailProvider] (
    -- Primary key with auto-generated sequential UUID
    [id]                                    UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    [userId]                                UNIQUEIDENTIFIER NULL,                     -- Ownership (NULL = system template)

    -- Data fields
    [name]                                  NVARCHAR(100) NOT NULL,                    -- Display name (e.g. Gmail)
    [imapHost]                              NVARCHAR(255) NOT NULL DEFAULT '',         -- IMAP server host
    [imapPort]                              INT NOT NULL DEFAULT 993 CHECK ([imapPort] BETWEEN 1 AND 65535), -- IMAP port
    [imapUseSsl]                            BIT DEFAULT 1,                             -- Use SSL/TLS for IMAP
    [smtpHost]                              NVARCHAR(255) NOT NULL DEFAULT '',         -- SMTP server host
    [smtpPort]                              INT NOT NULL DEFAULT 587 CHECK ([smtpPort] BETWEEN 1 AND 65535), -- SMTP port
    [smtpUseSsl]                            BIT DEFAULT 1,                             -- Use SSL/TLS for SMTP
    [isSystem]                              BIT DEFAULT 0,                             -- Seeded global template; app blocks edit/delete

    -- Status and lifecycle management
    [isActive]                               BIT DEFAULT 1,                            -- Paused by user; reversible (deactivate / reactivate)
    [isDeleted]                              BIT DEFAULT 0,                            -- Removed by user; hidden permanently; row retained

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

-- Index for listing a user's custom providers
CREATE INDEX [IX_EmailProvider_UserId] ON [dbo].[EmailProvider] ([userId]) WHERE [isSystem] = 0 AND [isDeleted] = 0;
GO

-- =====================================================
-- SEED DATA (optional — uncomment on new databases)
-- =====================================================
-- Well-known providers: [isSystem] = 1, [userId] = NULL (app blocks edit/delete).
-- Custom templates: created per user in app ([isSystem] = 0, [userId] = owner).
-- Run after CREATE TABLE and indexes. Idempotent by fixed [id] for system rows (cross-env debug).
-- GO

-- IF NOT EXISTS (SELECT 1 FROM [dbo].[EmailProvider] WHERE [id] = 'E1000001-0000-4000-8000-000000000001' AND [isDeleted] = 0)
-- BEGIN
--     INSERT INTO [dbo].[EmailProvider] (
--         [id], [name], [imapHost], [imapPort], [imapUseSsl],
--         [smtpHost], [smtpPort], [smtpUseSsl], [isSystem])
--     VALUES (
--         'E1000001-0000-4000-8000-000000000001', N'Gmail', N'imap.gmail.com', 993, 1,
--         N'smtp.gmail.com', 587, 1, 1);
-- END
-- GO

-- IF NOT EXISTS (SELECT 1 FROM [dbo].[EmailProvider] WHERE [id] = 'E1000002-0000-4000-8000-000000000002' AND [isDeleted] = 0)
-- BEGIN
--     INSERT INTO [dbo].[EmailProvider] (
--         [id], [name], [imapHost], [imapPort], [imapUseSsl],
--         [smtpHost], [smtpPort], [smtpUseSsl], [isSystem])
--     VALUES (
--         'E1000002-0000-4000-8000-000000000002', N'Outlook.com', N'imap-mail.outlook.com', 993, 1,
--         N'smtp-mail.outlook.com', 587, 1, 1);
-- END
-- GO

-- IF NOT EXISTS (SELECT 1 FROM [dbo].[EmailProvider] WHERE [id] = 'E1000003-0000-4000-8000-000000000003' AND [isDeleted] = 0)
-- BEGIN
--     INSERT INTO [dbo].[EmailProvider] (
--         [id], [name], [imapHost], [imapPort], [imapUseSsl],
--         [smtpHost], [smtpPort], [smtpUseSsl], [isSystem])
--     VALUES (
--         'E1000003-0000-4000-8000-000000000003', N'Yahoo Mail', N'imap.mail.yahoo.com', 993, 1,
--         N'smtp.mail.yahoo.com', 465, 1, 1);
-- END
-- GO

-- IF NOT EXISTS (SELECT 1 FROM [dbo].[EmailProvider] WHERE [id] = 'E1000004-0000-4000-8000-000000000004' AND [isDeleted] = 0)
-- BEGIN
--     INSERT INTO [dbo].[EmailProvider] (
--         [id], [name], [imapHost], [imapPort], [imapUseSsl],
--         [smtpHost], [smtpPort], [smtpUseSsl], [isSystem])
--     VALUES (
--         'E1000004-0000-4000-8000-000000000004', N'iCloud Mail', N'imap.mail.me.com', 993, 1,
--         N'smtp.mail.me.com', 587, 1, 1);
-- END
-- GO

-- IF NOT EXISTS (SELECT 1 FROM [dbo].[EmailProvider] WHERE [id] = 'E1000005-0000-4000-8000-000000000005' AND [isDeleted] = 0)
-- BEGIN
--     INSERT INTO [dbo].[EmailProvider] (
--         [id], [name], [imapHost], [imapPort], [imapUseSsl],
--         [smtpHost], [smtpPort], [smtpUseSsl], [isSystem])
--     VALUES (
--         'E1000005-0000-4000-8000-000000000005', N'Zoho Mail', N'imap.zoho.com', 993, 1,
--         N'smtp.zoho.com', 587, 1, 1);
-- END
-- GO

-- IF NOT EXISTS (SELECT 1 FROM [dbo].[EmailProvider] WHERE [id] = 'E1000006-0000-4000-8000-000000000006' AND [isDeleted] = 0)
-- BEGIN
--     INSERT INTO [dbo].[EmailProvider] (
--         [id], [name], [imapHost], [imapPort], [imapUseSsl],
--         [smtpHost], [smtpPort], [smtpUseSsl], [isSystem])
--     VALUES (
--         'E1000006-0000-4000-8000-000000000006', N'Fastmail', N'imap.fastmail.com', 993, 1,
--         N'smtp.fastmail.com', 465, 1, 1);
-- END
-- GO
