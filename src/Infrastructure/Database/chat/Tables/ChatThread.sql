-- =====================================================
-- CHAT THREAD TABLE
-- =====================================================
-- Per-user conversation threads (sidebar + agent selection).
-- Not workspace reference data or workflow rules.
--
-- Business Rules:
-- - Each chat thread belongs to exactly one user
-- - memorySummaryThroughMessageId FK to ChatMessage is applied in chat/Tables/ChatMessage.sql
-- - Apply after dbo.User ([chat] schema must already exist)
-- - All records include audit fields for tracking changes
-- =====================================================
GO

CREATE TABLE [chat].[ChatThread] (
    -- Primary key with auto-generated sequential UUID
    [id]                                    UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    [userId]                                UNIQUEIDENTIFIER NOT NULL,                 -- Owner (FK to User)

    -- Data fields
    [title]                                 NVARCHAR(200) NOT NULL,                    -- Sidebar / header title
    [chatAgent]                             INT NOT NULL DEFAULT 0,                    -- ChatAgent enum (Assistant = 0)

    [memorySummary]                         NVARCHAR(MAX) NULL,                        -- Rolling summary for long threads
    [memorySummaryThroughMessageId]         UNIQUEIDENTIFIER NULL,                     -- Last message id folded into summary

    -- Status and lifecycle management
    [isActive]                               BIT DEFAULT 1,                            -- Whether the thread row is active
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

-- Index for sidebar thread list by user
CREATE INDEX [IX_ChatThread_UserId] ON [chat].[ChatThread] ([userId]) WHERE [isDeleted] = 0;
GO

-- Index for filtering by active status
-- CREATE INDEX [IX_ChatThread_IsActive] ON [chat].[ChatThread] ([isActive]) WHERE [isActive] = 1;
-- GO

-- Index for filtering by deletion status
-- CREATE INDEX [IX_ChatThread_IsDeleted] ON [chat].[ChatThread] ([isDeleted]) WHERE [isDeleted] = 0;
-- GO

-- Composite index for active, non-deleted records
-- CREATE INDEX [IX_ChatThread_IsActive_IsDeleted] ON [chat].[ChatThread] ([isActive], [isDeleted]) WHERE [isActive] = 1 AND [isDeleted] = 0;
-- GO

-- Index for audit queries
-- CREATE INDEX [IX_ChatThread_CreatedAt] ON [chat].[ChatThread] ([createdAt] DESC);
-- GO

-- =====================================================
-- SEED DATA (optional — uncomment on new databases)
-- =====================================================
-- No default rows; threads are created in Chat UI.
-- GO
