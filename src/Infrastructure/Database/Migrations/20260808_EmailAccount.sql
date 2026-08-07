-- =====================================================
-- MIGRATION: EmailAccount (per-user connected inboxes)
-- =====================================================
-- Creates dbo.EmailAccount and backfills from UserSetting.EmailSettingsJson.
-- Keep CREATE TABLE in sync with Database/dbo/Tables/EmailAccount.sql.
-- =====================================================
GO

IF OBJECT_ID(N'dbo.EmailAccount', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[EmailAccount] (
        [id]                                     UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        [userId]                                 UNIQUEIDENTIFIER NOT NULL,
        [emailProviderId]                        UNIQUEIDENTIFIER NOT NULL,
        [alias]                                  NVARCHAR(64) NOT NULL,
        [emailAddress]                           NVARCHAR(255) NOT NULL,
        [username]                               NVARCHAR(255) NOT NULL,
        [password]                               NVARCHAR(512) NOT NULL,
        [isDefault]                              BIT NOT NULL DEFAULT 0,
        [sortOrder]                              INT NOT NULL DEFAULT 0,
        [isActive]                               BIT DEFAULT 1,
        [isDeleted]                              BIT DEFAULT 0,
        [createdBy]                              UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
        [createdAt]                              DATETIME2 DEFAULT SYSUTCDATETIME(),
        [updatedBy]                              UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
        [updatedAt]                              DATETIME2 DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [FK_EmailAccount_User] FOREIGN KEY ([userId]) REFERENCES [dbo].[User] ([id]),
        CONSTRAINT [FK_EmailAccount_EmailProvider] FOREIGN KEY ([emailProviderId]) REFERENCES [dbo].[EmailProvider] ([id])
    );

    CREATE UNIQUE INDEX [IX_EmailAccount_UserId_Alias]
        ON [dbo].[EmailAccount] ([userId], [alias]) WHERE [isDeleted] = 0;
    CREATE UNIQUE INDEX [IX_EmailAccount_UserId_IsDefault]
        ON [dbo].[EmailAccount] ([userId]) WHERE [isDefault] = 1 AND [isDeleted] = 0;
    CREATE UNIQUE INDEX [IX_EmailAccount_UserId_EmailAddress]
        ON [dbo].[EmailAccount] ([userId], [emailAddress]) WHERE [isDeleted] = 0;
    CREATE INDEX [IX_EmailAccount_UserId_SortOrder]
        ON [dbo].[EmailAccount] ([userId], [sortOrder]) WHERE [isDeleted] = 0;
    CREATE INDEX [IX_EmailAccount_EmailProviderId]
        ON [dbo].[EmailAccount] ([emailProviderId]) WHERE [isDeleted] = 0;
END
GO

-- =====================================================
-- BACKFILL: UserSetting.EmailSettingsJson → EmailAccount
-- =====================================================
IF COL_LENGTH(N'dbo.UserSetting', N'EmailSettingsJson') IS NOT NULL
BEGIN
    SET ANSI_NULLS ON;
    SET QUOTED_IDENTIFIER ON;

    DECLARE @CustomProviderId UNIQUEIDENTIFIER = 'a1000001-0001-4000-8000-000000000004';
    DECLARE @SystemUserId UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000000';

    INSERT INTO [dbo].[EmailAccount] (
        [userId],
        [emailProviderId],
        [alias],
        [emailAddress],
        [username],
        [password],
        [isDefault],
        [sortOrder],
        [isActive],
        [isDeleted],
        [createdBy],
        [updatedBy]
    )
    SELECT
        src.[userId],
        COALESCE(ep.[id], @CustomProviderId),
        N'Primary',
        LTRIM(RTRIM(src.[emailAddress])),
        LTRIM(RTRIM(src.[username])),
        src.[password],
        1,
        0,
        1,
        0,
        @SystemUserId,
        @SystemUserId
    FROM (
        SELECT
            us.[userId],
            j.[providerSlug],
            j.[provider],
            j.[emailAddress],
            j.[username],
            j.[password],
            LOWER(LTRIM(RTRIM(COALESCE(
                NULLIF(j.[providerSlug], N''),
                CASE WHEN LOWER(LTRIM(RTRIM(j.[provider]))) = N'gmail' THEN N'gmail' ELSE N'custom' END
            )))) AS [resolvedSlug]
        FROM [dbo].[UserSetting] us
        CROSS APPLY OPENJSON(us.[EmailSettingsJson]) WITH (
            [providerSlug] NVARCHAR(64) '$.providerSlug',
            [provider] NVARCHAR(32) '$.provider',
            [emailAddress] NVARCHAR(255) '$.emailAddress',
            [username] NVARCHAR(255) '$.username',
            [password] NVARCHAR(512) '$.password'
        ) j
        WHERE us.[isDeleted] = 0
          AND us.[isActive] = 1
          AND us.[EmailSettingsJson] IS NOT NULL
          AND ISJSON(us.[EmailSettingsJson]) = 1
          AND LTRIM(RTRIM(ISNULL(j.[emailAddress], N''))) <> N''
          AND LTRIM(RTRIM(ISNULL(j.[username], N''))) <> N''
          AND LTRIM(RTRIM(ISNULL(j.[password], N''))) <> N''
    ) src
    LEFT JOIN [dbo].[EmailProvider] ep
        ON ep.[slug] = src.[resolvedSlug] AND ep.[isDeleted] = 0
    WHERE NOT EXISTS (
        SELECT 1
        FROM [dbo].[EmailAccount] ea
        WHERE ea.[userId] = src.[userId] AND ea.[isDeleted] = 0
    );
END
GO
