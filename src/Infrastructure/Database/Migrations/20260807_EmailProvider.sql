-- =====================================================
-- MIGRATION: Email Provider catalog
-- =====================================================
-- Applies dbo/Tables/EmailProvider.sql to existing databases and seeds system providers.
-- Keep CREATE TABLE in sync with Database/dbo/Tables/EmailProvider.sql.
-- =====================================================
GO

IF OBJECT_ID(N'dbo.EmailProvider', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[EmailProvider] (
        -- Primary key with auto-generated sequential UUID
        [id]                                    UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),

        -- Data fields
        [name]                                  NVARCHAR(100) NOT NULL,
        [slug]                                  NVARCHAR(64) NOT NULL,
        [imapHost]                              NVARCHAR(255) NOT NULL DEFAULT '',
        [imapPort]                              INT NOT NULL DEFAULT 993,
        [imapUseSsl]                            BIT DEFAULT 1,
        [smtpHost]                              NVARCHAR(255) NOT NULL DEFAULT '',
        [smtpPort]                              INT NOT NULL DEFAULT 587,
        [smtpUseSsl]                            BIT DEFAULT 1,
        [setupHelpUrl]                          NVARCHAR(500) NULL,
        [sortOrder]                             INT NOT NULL DEFAULT 0,
        [isSystem]                              BIT DEFAULT 0,

        -- Status and lifecycle management
        [isActive]                               BIT DEFAULT 1,
        [isDeleted]                              BIT DEFAULT 0,

        -- Audit fields for tracking changes
        [createdBy]                              UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
        [createdAt]                              DATETIME2 DEFAULT SYSUTCDATETIME(),
        [updatedBy]                              UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
        [updatedAt]                              DATETIME2 DEFAULT SYSUTCDATETIME()
    );

    CREATE UNIQUE INDEX [IX_EmailProvider_Slug] ON [dbo].[EmailProvider] ([slug]) WHERE [isDeleted] = 0;
    CREATE INDEX [IX_EmailProvider_SortOrder] ON [dbo].[EmailProvider] ([sortOrder]) WHERE [isDeleted] = 0;
END
GO

-- =====================================================
-- EMAIL PROVIDER SEED DATA
-- =====================================================
MERGE [dbo].[EmailProvider] AS target
USING (VALUES
    ('a1000001-0001-4000-8000-000000000001', N'Gmail',                    N'gmail',   N'imap.gmail.com',        993, 1, N'smtp.gmail.com',       587, 1, N'https://support.google.com/mail/answer/185833', 10, 1),
    ('a1000001-0001-4000-8000-000000000002', N'Outlook / Microsoft 365', N'outlook', N'outlook.office365.com', 993, 1, N'smtp.office365.com',   587, 1, N'https://support.microsoft.com/office/outlook', 20, 1),
    ('a1000001-0001-4000-8000-000000000003', N'Yahoo Mail',               N'yahoo',   N'imap.mail.yahoo.com',   993, 1, N'smtp.mail.yahoo.com',  587, 1, NULL, 30, 1),
    ('a1000001-0001-4000-8000-000000000004', N'Custom',                   N'custom',  N'',                      993, 1, N'',                     587, 1, NULL, 100, 1)
) AS source (id, name, slug, imapHost, imapPort, imapUseSsl, smtpHost, smtpPort, smtpUseSsl, setupHelpUrl, sortOrder, isSystem)
ON target.id = source.id
WHEN NOT MATCHED THEN
    INSERT (id, name, slug, imapHost, imapPort, imapUseSsl, smtpHost, smtpPort, smtpUseSsl, setupHelpUrl, sortOrder, isSystem, isActive, isDeleted, createdBy, updatedBy)
    VALUES (source.id, source.name, source.slug, source.imapHost, source.imapPort, source.imapUseSsl, source.smtpHost, source.smtpPort, source.smtpUseSsl, source.setupHelpUrl, source.sortOrder, source.isSystem, 1, 0, '00000000-0000-0000-0000-000000000000', '00000000-0000-0000-0000-000000000000');
GO

-- Sort-order index (safe if table was created from an earlier revision of this migration)
IF OBJECT_ID(N'dbo.EmailProvider', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.indexes
       WHERE name = N'IX_EmailProvider_SortOrder'
         AND object_id = OBJECT_ID(N'dbo.EmailProvider'))
BEGIN
    CREATE INDEX [IX_EmailProvider_SortOrder] ON [dbo].[EmailProvider] ([sortOrder]) WHERE [isDeleted] = 0;
END
GO
