-- =====================================================
-- BACKLOG TABLE
-- =====================================================
-- This table stores unprocessed work items that can be moved to either Task or Issue tables.
-- 
-- Business Rules:
-- - Each backlog item belongs to exactly one project
-- - Backlog items are staging/placeholder entries before proper categorization
-- - Items can be moved to Task (planned work) or Issue (reactive work) tables
-- - Backlog items have minimal fields - details are added when moved to Task/Issue
-- - All records include audit fields for tracking changes
-- =====================================================

CREATE TABLE [dbo].[Backlog] (
    -- Primary key with auto-generated sequential UUID
    [id]                                    UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),

    -- Foreign keys
    [projectId]                             UNIQUEIDENTIFIER NOT NULL,                 -- Reference to Project

    -- Data fields
    [title]                                 NVARCHAR(500) NOT NULL,                    -- Backlog item title/name
    [description]                           NVARCHAR(MAX) NULL,                        -- Backlog item description/details
    -- [status]                                TINYINT NOT NULL DEFAULT 0,                -- Backlog status enum (commented - not used until moved to Task/Issue)
    [priority]                              TINYINT NULL,                              -- Backlog item priority (optional, can be 0-5 or similar scale)

    -- User assignments
    [requestedById]                         UNIQUEIDENTIFIER NULL,                     -- User who added/requested the backlog item (optional)
    -- [assignedToId]                          UNIQUEIDENTIFIER NULL,                     -- User assigned to work on the backlog item (commented - not assigned until moved to Task/Issue)

    -- Dates
    -- [startDate]                             DATETIME2 NULL,                            -- Start date (commented - set when moved to Task/Issue)
    -- [dueDate]                               DATETIME2 NULL,                            -- Due date (commented - set when moved to Task/Issue)
    -- [completedDate]                         DATETIME2 NULL,                            -- Completion date (commented - set when moved to Task/Issue)

    -- Status and lifecycle management
    [isActive]                              BIT DEFAULT 1,                             -- Whether the backlog item is active
    [isDeleted]                             BIT DEFAULT 0,                             -- Soft delete flag for data retention

    -- Audit fields for tracking changes
    [createdBy]                             UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [createdAt]                             DATETIME2 DEFAULT SYSUTCDATETIME(),
    [updatedBy]                             UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [updatedAt]                             DATETIME2 DEFAULT SYSUTCDATETIME(),

    -- Foreign key constraints
    CONSTRAINT [FK_Backlog_Project]         FOREIGN KEY ([projectId]) REFERENCES [dbo].[Project]([id]),
    CONSTRAINT [FK_Backlog_RequestedBy]      FOREIGN KEY ([requestedById]) REFERENCES [dbo].[User]([id])
    -- CONSTRAINT [FK_Backlog_AssignedTo]        FOREIGN KEY ([assignedToId]) REFERENCES [dbo].[User]([id])
);

-- =====================================================
-- INDEXES FOR BACKLOG TABLE
-- =====================================================

-- Index for filtering by project (find all backlog items in a project)
-- CREATE INDEX [IX_Backlog_ProjectId] ON [dbo].[Backlog] ([projectId]) WHERE [isActive] = 1 AND [isDeleted] = 0;

-- Index for filtering by requester (find all backlog items requested by a user)
-- CREATE INDEX [IX_Backlog_RequestedById] ON [dbo].[Backlog] ([requestedById]) WHERE [requestedById] IS NOT NULL AND [isActive] = 1 AND [isDeleted] = 0;

-- Index for filtering by assignee (find all backlog items assigned to a user)
-- CREATE INDEX [IX_Backlog_AssignedToId] ON [dbo].[Backlog] ([assignedToId]) WHERE [assignedToId] IS NOT NULL AND [isActive] = 1 AND [isDeleted] = 0;

-- Index for filtering by status (find backlog items by workflow status)
-- CREATE INDEX [IX_Backlog_Status] ON [dbo].[Backlog] ([status]) WHERE [isActive] = 1 AND [isDeleted] = 0;

-- Composite index for project and status queries
-- CREATE INDEX [IX_Backlog_ProjectId_Status] ON [dbo].[Backlog] ([projectId], [status]) WHERE [isActive] = 1 AND [isDeleted] = 0;

-- Index for start date queries (find backlog items by start date)
-- CREATE INDEX [IX_Backlog_StartDate] ON [dbo].[Backlog] ([startDate]) WHERE [startDate] IS NOT NULL AND [isActive] = 1 AND [isDeleted] = 0;

-- Index for due date queries (find overdue or upcoming backlog items)
-- CREATE INDEX [IX_Backlog_DueDate] ON [dbo].[Backlog] ([dueDate]) WHERE [dueDate] IS NOT NULL AND [isActive] = 1 AND [isDeleted] = 0;

-- Index for completed date queries (find backlog items by completion date)
-- CREATE INDEX [IX_Backlog_CompletedDate] ON [dbo].[Backlog] ([completedDate]) WHERE [completedDate] IS NOT NULL AND [isActive] = 1 AND [isDeleted] = 0;

-- Index for filtering by priority (find backlog items by priority level)
-- CREATE INDEX [IX_Backlog_Priority] ON [dbo].[Backlog] ([priority]) WHERE [priority] IS NOT NULL AND [isActive] = 1 AND [isDeleted] = 0;

-- Index for filtering by active status
-- CREATE INDEX [IX_Backlog_IsActive] ON [dbo].[Backlog] ([isActive]) WHERE [isActive] = 1;

-- Index for filtering by deletion status
-- CREATE INDEX [IX_Backlog_IsDeleted] ON [dbo].[Backlog] ([isDeleted]) WHERE [isDeleted] = 0;

-- Composite index for active, non-deleted records
-- CREATE INDEX [IX_Backlog_IsActive_IsDeleted] ON [dbo].[Backlog] ([isActive], [isDeleted]) WHERE [isActive] = 1 AND [isDeleted] = 0;

-- Index for audit queries
-- CREATE INDEX [IX_Backlog_CreatedAt] ON [dbo].[Backlog] ([createdAt] DESC);

-- Index for finding records by creator
-- CREATE INDEX [IX_Backlog_CreatedBy] ON [dbo].[Backlog] ([createdBy]);

-- Index for finding records by updater
-- CREATE INDEX [IX_Backlog_UpdatedBy] ON [dbo].[Backlog] ([updatedBy]);

-- Index for finding records by last update time
-- CREATE INDEX [IX_Backlog_UpdatedAt] ON [dbo].[Backlog] ([updatedAt] DESC);

