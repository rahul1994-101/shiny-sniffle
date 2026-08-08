-- =====================================================
-- CONTACT TABLE (workspace schema)
-- =====================================================
-- User-owned reference person for workflows and product features.
-- Not app login (dbo.User) and not a personal phonebook card.
--
-- Business Rules:
-- - Scoped to userId; soft delete via isDeleted
-- - displayName required; email and phone optional (use-case oriented)
-- - email unique per user among non-deleted rows when set
-- - source tracks manual vs future import / from-email promotion
-- Apply after dbo tables (FK to dbo.User). Creates [workspace] if missing.
-- =====================================================
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'workspace')
BEGIN
    EXEC(N'CREATE SCHEMA [workspace]');
END
GO

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
GO

CREATE UNIQUE INDEX [IX_Contact_UserId_Email]
    ON [workspace].[Contact] ([userId], [email])
    WHERE [isDeleted] = 0 AND [email] IS NOT NULL;
GO

CREATE INDEX [IX_Contact_UserId_SortOrder]
    ON [workspace].[Contact] ([userId], [sortOrder])
    WHERE [isDeleted] = 0;
GO
