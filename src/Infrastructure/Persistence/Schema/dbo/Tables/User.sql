-- =====================================================
-- USER TABLE
-- =====================================================
-- This table stores user accounts for the system.
-- 
-- Business Rules:
-- - Each user has a unique email address
-- - Login password is stored AES-encrypted (Base64); mailbox passwords live in workspace.EmailAccount
-- - All records include audit fields for tracking changes
-- =====================================================
GO

CREATE TABLE [dbo].[User] (
    -- Primary key with auto-generated sequential UUID
    [id]                                    UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),

    -- Data fields
    [firstName]                             NVARCHAR(50) NOT NULL,                     -- User's firstname
    [lastName]                              NVARCHAR(50) NOT NULL,                     -- User's lastname
    [email]                                 NVARCHAR(255) NOT NULL,                    -- User email (must be unique)
    [mobile]                                NVARCHAR(20) NULL,                         -- User mobile number (optional)
    [password]                              NVARCHAR(512) NOT NULL,                    -- AES-encrypted login password (Base64)

    -- Status and lifecycle management
    [isActive]                               BIT DEFAULT 1,                            -- Whether the user account is active
    [isDeleted]                              BIT DEFAULT 0,                            -- Soft delete flag for data retention

    -- Audit fields for tracking changes
    [createdBy]                              UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [createdAt]                              DATETIME2 DEFAULT SYSUTCDATETIME(),
    [updatedBy]                              UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [updatedAt]                              DATETIME2 DEFAULT SYSUTCDATETIME()
);
GO

-- =====================================================
-- INDEXES FOR USER TABLE
-- =====================================================

-- Unique index for email (enforces uniqueness)
CREATE UNIQUE INDEX [IX_User_Email] ON [dbo].[User] ([email]) WHERE [isDeleted] = 0;
GO

-- Index for email lookups (authentication queries)
-- CREATE INDEX [IX_User_Email_IsActive] ON [dbo].[User] ([email], [isActive]) WHERE [isActive] = 1 AND [isDeleted] = 0;
-- GO

-- Index for filtering by active status
-- CREATE INDEX [IX_User_IsActive] ON [dbo].[User] ([isActive]) WHERE [isActive] = 1;
-- GO

-- Index for filtering by deletion status
-- CREATE INDEX [IX_User_IsDeleted] ON [dbo].[User] ([isDeleted]) WHERE [isDeleted] = 0;
-- GO

-- Composite index for active, non-deleted records
-- CREATE INDEX [IX_User_IsActive_IsDeleted] ON [dbo].[User] ([isActive], [isDeleted]) WHERE [isActive] = 1 AND [isDeleted] = 0;
-- GO

-- Index for mobile number lookups (if used for authentication/contact)
-- CREATE INDEX [IX_User_Mobile] ON [dbo].[User] ([mobile]) WHERE [mobile] IS NOT NULL;
-- GO

-- Index for audit queries
-- CREATE INDEX [IX_User_CreatedAt] ON [dbo].[User] ([createdAt] DESC);
-- GO

-- Index for finding records by creator
-- CREATE INDEX [IX_User_CreatedBy] ON [dbo].[User] ([createdBy]);
-- GO

-- Index for finding records by updater
-- CREATE INDEX [IX_User_UpdatedBy] ON [dbo].[User] ([updatedBy]);
-- GO

-- Index for finding records by last update time
-- CREATE INDEX [IX_User_UpdatedAt] ON [dbo].[User] ([updatedAt] DESC);
-- GO

-- =====================================================
-- SEED DATA (optional — uncomment on new databases)
-- =====================================================
-- No default rows; users are created via app sign-up.
-- GO
