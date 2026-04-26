-- =====================================================
-- PROJECT TABLE
-- =====================================================
-- This table stores projects in the system.
-- 
-- Business Rules:
-- - Each project has a unique title
-- - All records include audit fields for tracking changes
-- =====================================================

CREATE TABLE [dbo].[Project] (
    -- Primary key with auto-generated sequential UUID
    [id]                                    UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),

    -- Data fields
    [title]                                 NVARCHAR(500) NOT NULL,                    -- Project title/name (must be unique)
    [description]                           NVARCHAR(MAX) NULL,                        -- Project description/details

    -- Status and lifecycle management
    [isActive]                               BIT DEFAULT 1,                            -- Whether the project is active
    [isDeleted]                              BIT DEFAULT 0,                            -- Soft delete flag for data retention

    -- Audit fields for tracking changes
    [createdBy]                              UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [createdAt]                              DATETIME2 DEFAULT SYSUTCDATETIME(),
    [updatedBy]                              UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [updatedAt]                              DATETIME2 DEFAULT SYSUTCDATETIME()
);

-- =====================================================
-- INDEXES FOR PROJECT TABLE
-- =====================================================

-- Unique index for title (enforces uniqueness)
CREATE UNIQUE INDEX [IX_Project_Title] ON [dbo].[Project] ([title]) WHERE [isDeleted] = 0;

-- Index for filtering by active status
-- CREATE INDEX [IX_Project_IsActive] ON [dbo].[Project] ([isActive]) WHERE [isActive] = 1;

-- Index for filtering by deletion status
-- CREATE INDEX [IX_Project_IsDeleted] ON [dbo].[Project] ([isDeleted]) WHERE [isDeleted] = 0;

-- Composite index for active, non-deleted records
-- CREATE INDEX [IX_Project_IsActive_IsDeleted] ON [dbo].[Project] ([isActive], [isDeleted]) WHERE [isActive] = 1 AND [isDeleted] = 0;

-- Index for audit queries
-- CREATE INDEX [IX_Project_CreatedAt] ON [dbo].[Project] ([createdAt] DESC);

-- Index for finding records by creator
-- CREATE INDEX [IX_Project_CreatedBy] ON [dbo].[Project] ([createdBy]);

-- Index for finding records by updater
-- CREATE INDEX [IX_Project_UpdatedBy] ON [dbo].[Project] ([updatedBy]);

-- Index for finding records by last update time
-- CREATE INDEX [IX_Project_UpdatedAt] ON [dbo].[Project] ([updatedAt] DESC);
