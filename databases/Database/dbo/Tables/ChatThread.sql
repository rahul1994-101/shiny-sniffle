-- =====================================================
-- CHAT THREAD TABLE
-- =====================================================
-- This table stores chat threads (conversations) owned by a user.
--
-- Business Rules:
-- - Each chat thread belongs to exactly one user
-- - A thread has a human-readable title shown in the sidebar
-- - All records include audit fields for tracking changes
-- =====================================================
GO

CREATE TABLE [dbo].[ChatThread] (
    -- Primary key with auto-generated sequential UUID
    [id]                                    UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    [userId]                                UNIQUEIDENTIFIER NOT NULL,                 -- Owner of the chat thread (FK to User)

    -- Data fields
    [title]                                 NVARCHAR(200) NOT NULL,                    -- Human-readable thread title
    [chatAgent]                             INT NOT NULL DEFAULT 0,                    -- ChatAgent enum: 0=Assistant, 1=Email, ...

    -- Thread memory (summary of messages beyond the short-term window)
    [memorySummary]                         NVARCHAR(MAX) NULL,
    [memorySummaryThroughMessageId]         UNIQUEIDENTIFIER NULL,

    -- Status and lifecycle management
    [isActive]                               BIT DEFAULT 1,                            -- Whether the chat thread is active
    [isDeleted]                              BIT DEFAULT 0,                            -- Soft delete flag for data retention

    -- Audit fields for tracking changes
    [createdBy]                              UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [createdAt]                              DATETIME2 DEFAULT SYSUTCDATETIME(),
    [updatedBy]                              UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [updatedAt]                              DATETIME2 DEFAULT SYSUTCDATETIME(),

    -- Foreign keys
    CONSTRAINT [FK_ChatThread_User] FOREIGN KEY ([userId]) REFERENCES [dbo].[User] ([id])
);
GO

-- =====================================================
-- INDEXES FOR CHAT THREAD TABLE
-- =====================================================

-- Index for listing a user's threads (sidebar lookups)
CREATE INDEX [IX_ChatThread_UserId] ON [dbo].[ChatThread] ([userId]) WHERE [isDeleted] = 0;
GO

-- Index for filtering by active status
-- CREATE INDEX [IX_ChatThread_IsActive] ON [dbo].[ChatThread] ([isActive]) WHERE [isActive] = 1;
-- GO

-- Index for filtering by deletion status
-- CREATE INDEX [IX_ChatThread_IsDeleted] ON [dbo].[ChatThread] ([isDeleted]) WHERE [isDeleted] = 0;
-- GO

-- Composite index for active, non-deleted records
-- CREATE INDEX [IX_ChatThread_IsActive_IsDeleted] ON [dbo].[ChatThread] ([isActive], [isDeleted]) WHERE [isActive] = 1 AND [isDeleted] = 0;
-- GO

-- Index for audit queries
-- CREATE INDEX [IX_ChatThread_CreatedAt] ON [dbo].[ChatThread] ([createdAt] DESC);
-- GO

-- Index for finding records by creator
-- CREATE INDEX [IX_ChatThread_CreatedBy] ON [dbo].[ChatThread] ([createdBy]);
-- GO

-- Index for finding records by updater
-- CREATE INDEX [IX_ChatThread_UpdatedBy] ON [dbo].[ChatThread] ([updatedBy]);
-- GO

-- Index for finding records by last update time
-- CREATE INDEX [IX_ChatThread_UpdatedAt] ON [dbo].[ChatThread] ([updatedAt] DESC);
-- GO
