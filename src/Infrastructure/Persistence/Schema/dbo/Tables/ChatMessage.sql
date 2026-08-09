-- =====================================================
-- CHAT MESSAGE TABLE
-- =====================================================
-- This table stores individual messages within a chat thread.
--
-- Business Rules:
-- - Each message belongs to exactly one chat thread
-- - Messages have a role (MAF ChatRole strings: user, assistant, system, tool) and textual content
-- - All records include audit fields for tracking changes
-- =====================================================
GO

CREATE TABLE [dbo].[ChatMessage] (
    -- Primary key with auto-generated sequential UUID
    [id]                                    UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    [chatThreadId]                          UNIQUEIDENTIFIER NOT NULL,                 -- Owning chat thread (FK to ChatThread)

    -- Data fields
    [role]                                  NVARCHAR(20) NOT NULL DEFAULT N'user',   -- MAF ChatRole: user, assistant, system, tool
    [content]                               NVARCHAR(MAX) NOT NULL,                    -- Message body

    -- Status and lifecycle management
    [isActive]                               BIT DEFAULT 1,                            -- Whether the message is active
    [isDeleted]                              BIT DEFAULT 0,                            -- Soft delete flag for data retention

    -- Audit fields for tracking changes
    [createdBy]                              UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [createdAt]                              DATETIME2 DEFAULT SYSUTCDATETIME(),
    [updatedBy]                              UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [updatedAt]                              DATETIME2 DEFAULT SYSUTCDATETIME(),

    -- Foreign keys
    CONSTRAINT [FK_ChatMessage_ChatThread] FOREIGN KEY ([chatThreadId]) REFERENCES [dbo].[ChatThread] ([id])
);
GO

-- =====================================================
-- INDEXES FOR CHAT MESSAGE TABLE
-- =====================================================

-- Index for listing a thread's messages (chat history queries)
CREATE INDEX [IX_ChatMessage_ChatThreadId] ON [dbo].[ChatMessage] ([chatThreadId]) WHERE [isDeleted] = 0;
GO

-- Index for filtering by active status
-- CREATE INDEX [IX_ChatMessage_IsActive] ON [dbo].[ChatMessage] ([isActive]) WHERE [isActive] = 1;
-- GO

-- Index for filtering by deletion status
-- CREATE INDEX [IX_ChatMessage_IsDeleted] ON [dbo].[ChatMessage] ([isDeleted]) WHERE [isDeleted] = 0;
-- GO

-- Composite index for active, non-deleted records
-- CREATE INDEX [IX_ChatMessage_IsActive_IsDeleted] ON [dbo].[ChatMessage] ([isActive], [isDeleted]) WHERE [isActive] = 1 AND [isDeleted] = 0;
-- GO

-- Index for audit queries
-- CREATE INDEX [IX_ChatMessage_CreatedAt] ON [dbo].[ChatMessage] ([createdAt] DESC);
-- GO

-- Index for finding records by creator
-- CREATE INDEX [IX_ChatMessage_CreatedBy] ON [dbo].[ChatMessage] ([createdBy]);
-- GO

-- Index for finding records by updater
-- CREATE INDEX [IX_ChatMessage_UpdatedBy] ON [dbo].[ChatMessage] ([updatedBy]);
-- GO

-- Index for finding records by last update time
-- CREATE INDEX [IX_ChatMessage_UpdatedAt] ON [dbo].[ChatMessage] ([updatedAt] DESC);
-- GO

-- =====================================================
-- SEED DATA (optional — uncomment on new databases)
-- =====================================================
-- No default rows; messages are created per chat thread.
-- GO
