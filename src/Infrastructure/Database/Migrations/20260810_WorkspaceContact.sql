-- =====================================================
-- MIGRATION: workspace.Contact + workflow schema shell
-- =====================================================
-- Creates workspace and workflow schemas; workspace.Contact table.
-- Keep CREATE TABLE in sync with Database/workspace/Tables/Contact.sql.
-- =====================================================

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'workspace')
BEGIN
    EXEC(N'CREATE SCHEMA [workspace]');
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'workflow')
BEGIN
    EXEC(N'CREATE SCHEMA [workflow]');
END
GO

IF OBJECT_ID(N'workspace.Contact', N'U') IS NULL
BEGIN
    CREATE TABLE [workspace].[Contact] (
        [id]                                     UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        [userId]                                 UNIQUEIDENTIFIER NOT NULL,
        [displayName]                            NVARCHAR(200) NOT NULL,
        [email]                                  NVARCHAR(255) NULL,
        [phone]                                  NVARCHAR(32) NULL,
        [notes]                                  NVARCHAR(2000) NULL,
        [source]                                 TINYINT NOT NULL DEFAULT 0,
        [sortOrder]                              INT NOT NULL DEFAULT 0,
        [isActive]                               BIT DEFAULT 1,
        [isDeleted]                              BIT DEFAULT 0,
        [createdBy]                              UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
        [createdAt]                              DATETIME2 DEFAULT SYSUTCDATETIME(),
        [updatedBy]                              UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
        [updatedAt]                              DATETIME2 DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [FK_Contact_User] FOREIGN KEY ([userId]) REFERENCES [dbo].[User] ([id])
    );

    CREATE UNIQUE INDEX [IX_Contact_UserId_Email]
        ON [workspace].[Contact] ([userId], [email])
        WHERE [isDeleted] = 0 AND [email] IS NOT NULL;

    CREATE INDEX [IX_Contact_UserId_SortOrder]
        ON [workspace].[Contact] ([userId], [sortOrder])
        WHERE [isDeleted] = 0;
END
GO
