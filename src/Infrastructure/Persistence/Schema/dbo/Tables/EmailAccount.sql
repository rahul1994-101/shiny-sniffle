-- =====================================================
-- EMAIL ACCOUNT TABLE
-- =====================================================
-- Per-user connected external inbox (Settings → Email → Accounts).
-- Not the same as dbo.User.email (app login identity).
--
-- Business Rules:
-- - alias NOT NULL; unique per user among non-deleted rows; optional in UI (auto-generated from email when blank)
-- - At most one row per user with isDefault = 1 among non-deleted rows
-- - IMAP/SMTP hosts come from dbo.EmailProvider via emailProviderId (not stored here in v1)
-- - Password is encrypted at rest
-- - All records include audit fields for tracking changes
-- =====================================================
GO

CREATE TABLE [dbo].[EmailAccount] (
    -- Primary key with auto-generated sequential UUID
    [id]                                     UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    [userId]                                 UNIQUEIDENTIFIER NOT NULL,                 -- Owner (FK to User)
    [emailProviderId]                        UNIQUEIDENTIFIER NOT NULL,                 -- Catalog row (FK to EmailProvider)

    -- Data fields
    [alias]                                  NVARCHAR(64) NOT NULL,                     -- Required; optional in UI (auto-generated from email when blank)
    [emailAddress]                           NVARCHAR(255) NOT NULL,                    -- Connected mailbox address
    [username]                               NVARCHAR(255) NOT NULL,                    -- IMAP/SMTP login
    [password]                               NVARCHAR(512) NOT NULL,                    -- Encrypted mailbox password
    [isDefault]                              BIT NOT NULL DEFAULT 0,                    -- Default for Email agent / settings
    [sortOrder]                              INT NOT NULL DEFAULT 0,                    -- List order in UI

    -- Status and lifecycle management
    [isActive]                               BIT DEFAULT 1,
    [isDeleted]                              BIT DEFAULT 0,

    -- Audit fields for tracking changes
    [createdBy]                              UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [createdAt]                              DATETIME2 DEFAULT SYSUTCDATETIME(),
    [updatedBy]                              UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [updatedAt]                              DATETIME2 DEFAULT SYSUTCDATETIME(),

    CONSTRAINT [FK_EmailAccount_User] FOREIGN KEY ([userId]) REFERENCES [dbo].[User] ([id]),
    CONSTRAINT [FK_EmailAccount_EmailProvider] FOREIGN KEY ([emailProviderId]) REFERENCES [dbo].[EmailProvider] ([id])
);
GO

-- =====================================================
-- INDEXES FOR EMAIL ACCOUNT TABLE
-- =====================================================

CREATE UNIQUE INDEX [IX_EmailAccount_UserId_Alias]
    ON [dbo].[EmailAccount] ([userId], [alias])
    WHERE [isDeleted] = 0;
GO

CREATE UNIQUE INDEX [IX_EmailAccount_UserId_IsDefault]
    ON [dbo].[EmailAccount] ([userId])
    WHERE [isDefault] = 1 AND [isDeleted] = 0;
GO

CREATE UNIQUE INDEX [IX_EmailAccount_UserId_EmailAddress]
    ON [dbo].[EmailAccount] ([userId], [emailAddress])
    WHERE [isDeleted] = 0;
GO

CREATE INDEX [IX_EmailAccount_UserId_SortOrder]
    ON [dbo].[EmailAccount] ([userId], [sortOrder])
    WHERE [isDeleted] = 0;
GO

CREATE INDEX [IX_EmailAccount_EmailProviderId]
    ON [dbo].[EmailAccount] ([emailProviderId])
    WHERE [isDeleted] = 0;
GO

-- =====================================================
-- SEED DATA (optional — uncomment on new databases)
-- =====================================================
-- No default rows; connections are created in Settings → Email → Accounts.
-- GO
