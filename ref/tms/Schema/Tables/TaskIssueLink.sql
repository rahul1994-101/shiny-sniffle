-- =====================================================
-- TASK ISSUE LINK TABLE
-- =====================================================
-- This table provides optional linking between Tasks and Issues.
-- 
-- Business Rules:
-- - One Task can link to many Issues (optional)
-- - One Issue can link to one Task (optional)
-- - This is a many-to-many relationship table
-- - All records include audit fields for tracking changes
-- =====================================================

CREATE TABLE [dbo].[TaskIssueLink] (
    -- Primary key with auto-generated sequential UUID
    [id]                                    UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),

    -- Foreign keys
    [taskId]                                UNIQUEIDENTIFIER NOT NULL,                 -- Reference to Task
    [issueId]                               UNIQUEIDENTIFIER NOT NULL,                 -- Reference to Issue

    -- Status and lifecycle management
    [isActive]                              BIT DEFAULT 1,                             -- Whether the link is active
    [isDeleted]                             BIT DEFAULT 0,                             -- Soft delete flag for data retention

    -- Audit fields for tracking changes
    [createdBy]                             UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [createdAt]                             DATETIME2 DEFAULT SYSUTCDATETIME(),
    [updatedBy]                             UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [updatedAt]                             DATETIME2 DEFAULT SYSUTCDATETIME(),

    -- Foreign key constraints
    CONSTRAINT [FK_TaskIssueLink_Task]      FOREIGN KEY ([taskId]) REFERENCES [dbo].[Task]([id]),
    CONSTRAINT [FK_TaskIssueLink_Issue]     FOREIGN KEY ([issueId]) REFERENCES [dbo].[Issue]([id])
);

-- =====================================================
-- INDEXES FOR TASK ISSUE LINK TABLE
-- =====================================================

-- Unique index for task-issue combination (prevents duplicate links)
-- CREATE UNIQUE INDEX [IX_TaskIssueLink_Task_Issue] ON [dbo].[TaskIssueLink] ([taskId], [issueId]) WHERE [isDeleted] = 0;

-- Index for filtering by task (find all issues linked to a task)
-- CREATE INDEX [IX_TaskIssueLink_TaskId] ON [dbo].[TaskIssueLink] ([taskId]) WHERE [isActive] = 1 AND [isDeleted] = 0;

-- Index for filtering by issue (find the task linked to an issue)
-- CREATE INDEX [IX_TaskIssueLink_IssueId] ON [dbo].[TaskIssueLink] ([issueId]) WHERE [isActive] = 1 AND [isDeleted] = 0;

-- Index for filtering by active status
-- CREATE INDEX [IX_TaskIssueLink_IsActive] ON [dbo].[TaskIssueLink] ([isActive]) WHERE [isActive] = 1;

-- Index for filtering by deletion status
-- CREATE INDEX [IX_TaskIssueLink_IsDeleted] ON [dbo].[TaskIssueLink] ([isDeleted]) WHERE [isDeleted] = 0;

-- Composite index for active, non-deleted links
-- CREATE INDEX [IX_TaskIssueLink_IsActive_IsDeleted] ON [dbo].[TaskIssueLink] ([isActive], [isDeleted]) WHERE [isActive] = 1 AND [isDeleted] = 0;

-- Index for audit queries
-- CREATE INDEX [IX_TaskIssueLink_CreatedAt] ON [dbo].[TaskIssueLink] ([createdAt] DESC);

-- Index for finding records by creator
-- CREATE INDEX [IX_TaskIssueLink_CreatedBy] ON [dbo].[TaskIssueLink] ([createdBy]);

-- Index for finding records by updater
-- CREATE INDEX [IX_TaskIssueLink_UpdatedBy] ON [dbo].[TaskIssueLink] ([updatedBy]);

-- Index for finding records by last update time
-- CREATE INDEX [IX_TaskIssueLink_UpdatedAt] ON [dbo].[TaskIssueLink] ([updatedAt] DESC);

