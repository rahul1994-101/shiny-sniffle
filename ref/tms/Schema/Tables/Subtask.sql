-- =====================================================
-- SUBTASK TABLE
-- =====================================================
-- This table stores lightweight subtasks (checklist items) for tasks.
-- 
-- Business Rules:
-- - Each subtask belongs to exactly one parent task
-- - Subtasks are simple checklist items (lightweight)
-- - Subtasks have a simple completed/not completed state
-- - All records include audit fields for tracking changes
-- =====================================================

CREATE TABLE [dbo].[Subtask] (
    -- Primary key with auto-generated sequential UUID
    [id]                                    UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),

    -- projectId should be here for better lokups

    -- Foreign keys
    [taskId]                                UNIQUEIDENTIFIER NOT NULL,                 -- Reference to parent Task

    -- Data fields
    [title]                                 NVARCHAR(500) NOT NULL,                    -- Subtask title/description
    [isCompleted]                           BIT DEFAULT 0,                             -- Whether the subtask is completed
    [orderIndex]                            INT DEFAULT 0,                             -- Display order within the parent task

    -- Status and lifecycle management
    [isActive]                              BIT DEFAULT 1,                             -- Whether the subtask is active
    [isDeleted]                             BIT DEFAULT 0,                             -- Soft delete flag for data retention

    -- Audit fields for tracking changes
    [createdBy]                             UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [createdAt]                             DATETIME2 DEFAULT SYSUTCDATETIME(),
    [updatedBy]                             UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [updatedAt]                             DATETIME2 DEFAULT SYSUTCDATETIME(),

    -- Foreign key constraints
    CONSTRAINT [FK_Subtask_Task]           FOREIGN KEY ([taskId]) REFERENCES [dbo].[Task]([id])
);

-- =====================================================
-- INDEXES FOR SUBTASK TABLE
-- =====================================================

-- Index for filtering by task (find all subtasks for a task)
-- CREATE INDEX [IX_Subtask_TaskId] ON [dbo].[Subtask] ([taskId]) WHERE [isActive] = 1 AND [isDeleted] = 0;

-- Composite index for task and completion status
-- CREATE INDEX [IX_Subtask_TaskId_IsCompleted] ON [dbo].[Subtask] ([taskId], [isCompleted]) WHERE [isActive] = 1 AND [isDeleted] = 0;

-- Index for ordering subtasks within a task
-- CREATE INDEX [IX_Subtask_TaskId_OrderIndex] ON [dbo].[Subtask] ([taskId], [orderIndex]) WHERE [isActive] = 1 AND [isDeleted] = 0;

-- Index for filtering by active status
-- CREATE INDEX [IX_Subtask_IsActive] ON [dbo].[Subtask] ([isActive]) WHERE [isActive] = 1;

-- Index for filtering by deletion status
-- CREATE INDEX [IX_Subtask_IsDeleted] ON [dbo].[Subtask] ([isDeleted]) WHERE [isDeleted] = 0;

-- Composite index for active, non-deleted records
-- CREATE INDEX [IX_Subtask_IsActive_IsDeleted] ON [dbo].[Subtask] ([isActive], [isDeleted]) WHERE [isActive] = 1 AND [isDeleted] = 0;

-- Index for audit queries
-- CREATE INDEX [IX_Subtask_CreatedAt] ON [dbo].[Subtask] ([createdAt] DESC);

-- Index for finding records by creator
-- CREATE INDEX [IX_Subtask_CreatedBy] ON [dbo].[Subtask] ([createdBy]);

-- Index for finding records by updater
-- CREATE INDEX [IX_Subtask_UpdatedBy] ON [dbo].[Subtask] ([updatedBy]);

-- Index for finding records by last update time
-- CREATE INDEX [IX_Subtask_UpdatedAt] ON [dbo].[Subtask] ([updatedAt] DESC);

