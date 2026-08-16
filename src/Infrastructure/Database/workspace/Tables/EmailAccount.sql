-- =====================================================
-- EMAIL ACCOUNT TABLE
-- =====================================================
-- Per-user connected external inbox (Workspace → Email accounts; Email agent / IMAP).
-- Not workflow data. Not the same as dbo.User.email (app login identity).
--
-- Business Rules:
-- - alias + context: see Agent reference section (alias NOT NULL; optional in UI with auto-generate)
-- - At most one row per user with isDefault = 1 among non-deleted rows
-- - IMAP/SMTP hosts come from dbo.EmailProvider via emailProviderId (not stored here in v1)
-- - Password is encrypted at rest
-- - isDeleted: user removed the row — gone from UI; retained internally (soft delete)
-- - isActive: user paused the row — reversible; excluded from mail/runtime until reactivated
-- - Apply after dbo.User, dbo.EmailProvider, and workspace/Tables/Contact.sql
-- - All records include audit fields for tracking changes
-- =====================================================
GO

CREATE TABLE [workspace].[EmailAccount] (
    -- Primary key with auto-generated sequential UUID
    [id]                                     UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    [userId]                                 UNIQUEIDENTIFIER NOT NULL,                 -- Owner (FK to User)
    [emailProviderId]                        UNIQUEIDENTIFIER NOT NULL,                 -- Catalog row (FK to EmailProvider)

    -- Data fields
    [emailAddress]                           NVARCHAR(255) NOT NULL,                    -- Connected mailbox address
    [username]                               NVARCHAR(255) NOT NULL,                    -- IMAP/SMTP login
    [password]                               NVARCHAR(512) NOT NULL,                    -- Encrypted mailbox password
    [isDefault]                              BIT NOT NULL DEFAULT 0,                    -- Default for Email agent

    -- Agent reference (alias + context for tools and prompts)
    [alias]                                  NVARCHAR(64) NOT NULL,                     -- Per-user handle (NOT NULL); optional in UI; auto-generated from email address when blank
    [context]                                NVARCHAR(2000) NULL,                       -- Optional facts for the UI and agent prompts

    -- Status and lifecycle management
    [isActive]                               BIT DEFAULT 1,                             -- Paused by user; reversible (deactivate / reactivate)
    [isDeleted]                              BIT DEFAULT 0,                             -- Removed by user; hidden permanently; row retained

    -- Audit fields for tracking changes
    [createdBy]                              UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [createdAt]                              DATETIME2 DEFAULT SYSUTCDATETIME(),
    [updatedBy]                              UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [updatedAt]                              DATETIME2 DEFAULT SYSUTCDATETIME(),

    -- Foreign keys
    CONSTRAINT [FK_EmailAccount_User] FOREIGN KEY ([userId]) REFERENCES [dbo].[User] ([id]),
    CONSTRAINT [FK_EmailAccount_EmailProvider] FOREIGN KEY ([emailProviderId]) REFERENCES [dbo].[EmailProvider] ([id])
);
GO

-- =====================================================
-- INDEXES FOR EMAIL ACCOUNT TABLE
-- =====================================================

-- Unique alias per user among non-deleted rows
CREATE UNIQUE INDEX [IX_EmailAccount_UserId_Alias] ON [workspace].[EmailAccount] ([userId], [alias]) WHERE [isDeleted] = 0;
GO

-- At most one default mailbox per user
CREATE UNIQUE INDEX [IX_EmailAccount_UserId_IsDefault] ON [workspace].[EmailAccount] ([userId]) WHERE [isDefault] = 1 AND [isDeleted] = 0;
GO

-- Unique email address per user among non-deleted rows
CREATE UNIQUE INDEX [IX_EmailAccount_UserId_EmailAddress] ON [workspace].[EmailAccount] ([userId], [emailAddress]) WHERE [isDeleted] = 0;
GO

-- Index for lookups by email provider
CREATE INDEX [IX_EmailAccount_EmailProviderId] ON [workspace].[EmailAccount] ([emailProviderId]) WHERE [isDeleted] = 0;
GO

-- =====================================================
-- SEED DATA (optional — uncomment on new databases)
-- =====================================================
-- No default rows; connections are created in Workspace.
-- GO
