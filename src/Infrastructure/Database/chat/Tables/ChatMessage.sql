-- =====================================================
-- CHAT MESSAGE TABLE
-- =====================================================
-- Messages within a chat thread (MAF roles: user, assistant, system, tool).
--
-- Business Rules:
-- - Each message belongs to exactly one chat thread
-- - Adds FK from ChatThread.memorySummaryThroughMessageId after this table exists
-- - Apply after chat/Tables/ChatThread.sql
-- - All records include audit fields for tracking changes
-- =====================================================
GO

CREATE TABLE [chat].[ChatMessage] (
    -- Primary key with auto-generated sequential UUID
    [id]                                    UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    [chatThreadId]                          UNIQUEIDENTIFIER NOT NULL,                 -- Parent thread (FK to ChatThread)

    -- Data fields
    [role]                                  NVARCHAR(20) NOT NULL DEFAULT N'user',     -- user | assistant | system | tool
    [content]                               NVARCHAR(MAX) NOT NULL,                    -- Message body

    -- Status and lifecycle management
    [isActive]                               BIT DEFAULT 1,                            -- Whether the message row is active
    [isDeleted]                              BIT DEFAULT 0,                            -- Soft delete flag for data retention

    -- Audit fields for tracking changes
    [createdBy]                              UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [createdAt]                              DATETIME2 DEFAULT SYSUTCDATETIME(),
    [updatedBy]                              UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [updatedAt]                              DATETIME2 DEFAULT SYSUTCDATETIME(),

    -- Foreign keys
    CONSTRAINT [FK_ChatMessage_ChatThread] FOREIGN KEY ([chatThreadId]) REFERENCES [chat].[ChatThread] ([id])
);
GO

-- =====================================================
-- FOREIGN KEYS (deferred — ChatThread.memorySummaryThroughMessageId)
-- =====================================================

-- Last message id folded into rolling memory summary
IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_ChatThread_MemorySummaryThroughMessage'
      AND parent_object_id = OBJECT_ID(N'[chat].[ChatThread]'))
BEGIN
    ALTER TABLE [chat].[ChatThread]
        ADD CONSTRAINT [FK_ChatThread_MemorySummaryThroughMessage]
            FOREIGN KEY ([memorySummaryThroughMessageId]) REFERENCES [chat].[ChatMessage] ([id]) ON DELETE SET NULL;
END
GO

-- =====================================================
-- INDEXES FOR CHAT MESSAGE TABLE
-- =====================================================

-- Index for loading messages in a thread
CREATE INDEX [IX_ChatMessage_ChatThreadId] ON [chat].[ChatMessage] ([chatThreadId]) WHERE [isDeleted] = 0;
GO

-- Index for filtering by active status
-- CREATE INDEX [IX_ChatMessage_IsActive] ON [chat].[ChatMessage] ([isActive]) WHERE [isActive] = 1;
-- GO

-- Index for filtering by deletion status
-- CREATE INDEX [IX_ChatMessage_IsDeleted] ON [chat].[ChatMessage] ([isDeleted]) WHERE [isDeleted] = 0;
-- GO

-- Composite index for active, non-deleted records
-- CREATE INDEX [IX_ChatMessage_IsActive_IsDeleted] ON [chat].[ChatMessage] ([isActive], [isDeleted]) WHERE [isActive] = 1 AND [isDeleted] = 0;
-- GO

-- Index for audit queries
-- CREATE INDEX [IX_ChatMessage_CreatedAt] ON [chat].[ChatMessage] ([createdAt] DESC);
-- GO

-- =====================================================
-- SEED DATA (optional — uncomment on new databases)
-- =====================================================
-- No default rows; messages are created per thread.
-- GO
