-- =====================================================
-- ISSUE TABLE
-- =====================================================
-- This table stores unplanned/reactive work items (bugs, incidents, small tasks) within projects.
-- 
-- Business Rules:
-- - Each issue belongs to exactly one project
-- - Issues have a lightweight lifecycle workflow (Open, Investigating, Resolved, Closed)
-- - Issues can optionally link to one task (via TaskIssueLink table)
-- - All records include audit fields for tracking changes
-- =====================================================

CREATE TABLE [dbo].[Issue] (
    -- Primary key with auto-generated sequential UUID
    [id]                                    UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),

    -- Foreign keys
    [projectId]                             UNIQUEIDENTIFIER NOT NULL,                 -- Reference to Project

    -- Data fields
    [title]                                 NVARCHAR(500) NOT NULL,                    -- Issue title/name
    [description]                           NVARCHAR(MAX) NULL,                        -- Issue description/details
    [status]                                TINYINT NOT NULL DEFAULT 0,                -- Issue status enum: 0 = Open, 1 = Investigating, 2 = Resolved, 3 = Closed
    [priority]                              TINYINT NULL,                              -- Issue priority (optional, can be 0-5 or similar scale)

    -- User assignments
    [requestedById]                         UNIQUEIDENTIFIER NULL,                     -- User who requested/reported the issue (optional)
    [assignedToId]                          UNIQUEIDENTIFIER NULL,                     -- User assigned to work on the issue (optional)

    -- Dates
    [startDate]                             DATETIME2 NULL,                            -- Start date (optional)
    [dueDate]                               DATETIME2 NULL,                            -- Due date (optional)
    [completedDate]                         DATETIME2 NULL,                            -- Completion date (optional)

    -- Status and lifecycle management
    [isActive]                              BIT DEFAULT 1,                             -- Whether the issue is active
    [isDeleted]                             BIT DEFAULT 0,                             -- Soft delete flag for data retention

    -- Audit fields for tracking changes
    [createdBy]                             UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [createdAt]                             DATETIME2 DEFAULT SYSUTCDATETIME(),
    [updatedBy]                             UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [updatedAt]                             DATETIME2 DEFAULT SYSUTCDATETIME(),

    -- Foreign key constraints
    CONSTRAINT [FK_Issue_Project]           FOREIGN KEY ([projectId]) REFERENCES [dbo].[Project]([id]),
    CONSTRAINT [FK_Issue_RequestedBy]       FOREIGN KEY ([requestedById]) REFERENCES [dbo].[User]([id]),
    CONSTRAINT [FK_Issue_AssignedTo]        FOREIGN KEY ([assignedToId]) REFERENCES [dbo].[User]([id])
);

-- =====================================================
-- INDEXES FOR ISSUE TABLE
-- =====================================================

-- Index for filtering by project (find all issues in a project)
-- CREATE INDEX [IX_Issue_ProjectId] ON [dbo].[Issue] ([projectId]) WHERE [isActive] = 1 AND [isDeleted] = 0;

-- Index for filtering by requester (find all issues requested by a user)
-- CREATE INDEX [IX_Issue_RequestedById] ON [dbo].[Issue] ([requestedById]) WHERE [requestedById] IS NOT NULL AND [isActive] = 1 AND [isDeleted] = 0;

-- Index for filtering by assignee (find all issues assigned to a user)
-- CREATE INDEX [IX_Issue_AssignedToId] ON [dbo].[Issue] ([assignedToId]) WHERE [assignedToId] IS NOT NULL AND [isActive] = 1 AND [isDeleted] = 0;

-- Index for filtering by status (find issues by workflow status)
-- CREATE INDEX [IX_Issue_Status] ON [dbo].[Issue] ([status]) WHERE [isActive] = 1 AND [isDeleted] = 0;

-- Composite index for project and status queries
-- CREATE INDEX [IX_Issue_ProjectId_Status] ON [dbo].[Issue] ([projectId], [status]) WHERE [isActive] = 1 AND [isDeleted] = 0;

-- Index for start date queries (find issues by start date)
-- CREATE INDEX [IX_Issue_StartDate] ON [dbo].[Issue] ([startDate]) WHERE [startDate] IS NOT NULL AND [isActive] = 1 AND [isDeleted] = 0;

-- Index for due date queries (find overdue or upcoming issues)
-- CREATE INDEX [IX_Issue_DueDate] ON [dbo].[Issue] ([dueDate]) WHERE [dueDate] IS NOT NULL AND [isActive] = 1 AND [isDeleted] = 0;

-- Index for completed date queries (find issues by completion date)
-- CREATE INDEX [IX_Issue_CompletedDate] ON [dbo].[Issue] ([completedDate]) WHERE [completedDate] IS NOT NULL AND [isActive] = 1 AND [isDeleted] = 0;

-- Index for filtering by priority (find issues by priority level)
-- CREATE INDEX [IX_Issue_Priority] ON [dbo].[Issue] ([priority]) WHERE [priority] IS NOT NULL AND [isActive] = 1 AND [isDeleted] = 0;

-- Index for filtering by active status
-- CREATE INDEX [IX_Issue_IsActive] ON [dbo].[Issue] ([isActive]) WHERE [isActive] = 1;

-- Index for filtering by deletion status
-- CREATE INDEX [IX_Issue_IsDeleted] ON [dbo].[Issue] ([isDeleted]) WHERE [isDeleted] = 0;

-- Composite index for active, non-deleted records
-- CREATE INDEX [IX_Issue_IsActive_IsDeleted] ON [dbo].[Issue] ([isActive], [isDeleted]) WHERE [isActive] = 1 AND [isDeleted] = 0;

-- Index for audit queries
-- CREATE INDEX [IX_Issue_CreatedAt] ON [dbo].[Issue] ([createdAt] DESC);

-- Index for finding records by creator
-- CREATE INDEX [IX_Issue_CreatedBy] ON [dbo].[Issue] ([createdBy]);

-- Index for finding records by updater
-- CREATE INDEX [IX_Issue_UpdatedBy] ON [dbo].[Issue] ([updatedBy]);

-- Index for finding records by last update time
-- CREATE INDEX [IX_Issue_UpdatedAt] ON [dbo].[Issue] ([updatedAt] DESC);

