-- =====================================================
-- TASK TABLE
-- =====================================================
-- This table stores planned work items (tasks) within projects.
-- 
-- Business Rules:
-- - Each task belongs to exactly one project
-- - Tasks have a richer lifecycle workflow (Todo, In Progress, Review, Done)
-- - Tasks can have subtasks (stored in Subtask table)
-- - Tasks can link to multiple issues (via TaskIssueLink table)
-- - All records include audit fields for tracking changes
-- =====================================================

CREATE TABLE [dbo].[Task] (
    -- Primary key with auto-generated sequential UUID
    [id]                                    UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),

    -- Foreign keys
    [projectId]                             UNIQUEIDENTIFIER NOT NULL,                 -- Reference to Project

    -- Data fields
    [title]                                 NVARCHAR(500) NOT NULL,                    -- Task title/name
    [description]                           NVARCHAR(MAX) NULL,                        -- Task description/details
    [status]                                TINYINT NOT NULL DEFAULT 0,                -- Task status enum: 0 = Todo, 1 = InProgress, 2 = Review, 3 = Done
    [priority]                              TINYINT NULL,                              -- Task priority (optional, can be 0-5 or similar scale)

    -- User assignments
    [requestedById]                         UNIQUEIDENTIFIER NULL,                     -- User who requested/created the task (optional)
    [assignedToId]                          UNIQUEIDENTIFIER NULL,                     -- User assigned to work on the task (optional)

    -- Dates
    [startDate]                             DATETIME2 NULL,                            -- Start date (optional)
    [dueDate]                               DATETIME2 NULL,                            -- Due date (optional)
    [completedDate]                         DATETIME2 NULL,                            -- Completion date (optional)

    -- Status and lifecycle management
    [isActive]                              BIT DEFAULT 1,                             -- Whether the task is active
    [isDeleted]                             BIT DEFAULT 0,                             -- Soft delete flag for data retention

    -- Audit fields for tracking changes
    [createdBy]                             UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [createdAt]                             DATETIME2 DEFAULT SYSUTCDATETIME(),
    [updatedBy]                             UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [updatedAt]                             DATETIME2 DEFAULT SYSUTCDATETIME(),

    -- Foreign key constraints
    CONSTRAINT [FK_Task_Project]            FOREIGN KEY ([projectId])  REFERENCES [dbo].[Project]([id]),
    CONSTRAINT [FK_Task_RequestedBy]       FOREIGN KEY ([requestedById]) REFERENCES [dbo].[User]([id]),
    CONSTRAINT [FK_Task_AssignedTo]        FOREIGN KEY ([assignedToId]) REFERENCES [dbo].[User]([id])
);

-- =====================================================
-- INDEXES FOR TASK TABLE
-- =====================================================

-- Index for filtering by project (find all tasks in a project)
-- CREATE INDEX [IX_Task_ProjectId] ON [dbo].[Task] ([projectId]) WHERE [isActive] = 1 AND [isDeleted] = 0;

-- Index for filtering by requester (find all tasks requested by a user)
-- CREATE INDEX [IX_Task_RequestedById] ON [dbo].[Task] ([requestedById]) WHERE [requestedById] IS NOT NULL AND [isActive] = 1 AND [isDeleted] = 0;

-- Index for filtering by assignee (find all tasks assigned to a user)
-- CREATE INDEX [IX_Task_AssignedToId] ON [dbo].[Task] ([assignedToId]) WHERE [assignedToId] IS NOT NULL AND [isActive] = 1 AND [isDeleted] = 0;

-- Index for filtering by status (find tasks by workflow status)
-- CREATE INDEX [IX_Task_Status] ON [dbo].[Task] ([status]) WHERE [isActive] = 1 AND [isDeleted] = 0;

-- Composite index for project and status queries
-- CREATE INDEX [IX_Task_ProjectId_Status] ON [dbo].[Task] ([projectId], [status]) WHERE [isActive] = 1 AND [isDeleted] = 0;

-- Index for start date queries (find tasks by start date)
-- CREATE INDEX [IX_Task_StartDate] ON [dbo].[Task] ([startDate]) WHERE [startDate] IS NOT NULL AND [isActive] = 1 AND [isDeleted] = 0;

-- Index for due date queries (find overdue or upcoming tasks)
-- CREATE INDEX [IX_Task_DueDate] ON [dbo].[Task] ([dueDate]) WHERE [dueDate] IS NOT NULL AND [isActive] = 1 AND [isDeleted] = 0;

-- Index for completed date queries (find tasks by completion date)
-- CREATE INDEX [IX_Task_CompletedDate] ON [dbo].[Task] ([completedDate]) WHERE [completedDate] IS NOT NULL AND [isActive] = 1 AND [isDeleted] = 0;

-- Index for filtering by priority (find tasks by priority level)
-- CREATE INDEX [IX_Task_Priority] ON [dbo].[Task] ([priority]) WHERE [priority] IS NOT NULL AND [isActive] = 1 AND [isDeleted] = 0;

-- Index for filtering by active status
-- CREATE INDEX [IX_Task_IsActive] ON [dbo].[Task] ([isActive]) WHERE [isActive] = 1;

-- Index for filtering by deletion status
-- CREATE INDEX [IX_Task_IsDeleted] ON [dbo].[Task] ([isDeleted]) WHERE [isDeleted] = 0;

-- Composite index for active, non-deleted records
-- CREATE INDEX [IX_Task_IsActive_IsDeleted] ON [dbo].[Task] ([isActive], [isDeleted]) WHERE [isActive] = 1 AND [isDeleted] = 0;

-- Index for audit queries
-- CREATE INDEX [IX_Task_CreatedAt] ON [dbo].[Task] ([createdAt] DESC);

-- Index for finding records by creator
-- CREATE INDEX [IX_Task_CreatedBy] ON [dbo].[Task] ([createdBy]);

-- Index for finding records by updater
-- CREATE INDEX [IX_Task_UpdatedBy] ON [dbo].[Task] ([updatedBy]);

-- Index for finding records by last update time
-- CREATE INDEX [IX_Task_UpdatedAt] ON [dbo].[Task] ([updatedAt] DESC);

