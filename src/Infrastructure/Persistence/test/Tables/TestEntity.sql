-- =====================================================
-- TEST ENTITY TABLE
-- =====================================================
-- Reference table for SQL patterns (procs, views, functions, triggers).
-- Not mapped in AppDbContext — for copy-paste examples only.
--
-- Business Rules:
-- - Each record has a unique name
-- - All records include audit fields for tracking changes
-- - Apply after test/Schema.sql
-- =====================================================
GO

CREATE TABLE test.TestEntity (
    -- Primary key with auto-generated sequential UUID
    [id]                                    UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),

    -- Data fields
    [name]                                  NVARCHAR(100) NOT NULL,                   -- Test name (must be unique)

    -- Status and lifecycle management
    [isActive]                              BIT DEFAULT 1,                            -- Whether the record is active
    [isDeleted]                             BIT DEFAULT 0,                            -- Soft delete flag for data retention  

    -- Audit fields for tracking changes
    [createdBy]                             UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [createdAt]                             DATETIME2 DEFAULT SYSUTCDATETIME(),
    [updatedBy]                             UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    [updatedAt]                             DATETIME2 DEFAULT SYSUTCDATETIME()
);
GO

-- =====================================================
-- INDEXES FOR TEST ENTITY TABLE
-- =====================================================

-- Index for quick lookups by name (most common query)
CREATE INDEX IX_TestEntity_Name ON test.TestEntity (name);
GO

-- Index for filtering by active status (most common business query)
-- CREATE INDEX IX_TestEntity_IsActive ON test.TestEntity (isActive) WHERE isActive = 1;
-- GO

-- Index for filtering by deletion status (soft delete queries)
-- CREATE INDEX IX_TestEntity_IsDeleted ON test.TestEntity (isDeleted) WHERE isDeleted = 0;
-- GO

-- Composite index for active, non-deleted records (business queries)
-- CREATE INDEX IX_TestEntity_IsActive_IsDeleted ON test.TestEntity (isActive, isDeleted) WHERE isActive = 1 AND isDeleted = 0;
-- GO

-- Index for audit queries (finding records by creation/update time)
-- CREATE INDEX IX_TestEntity_CreatedAt ON test.TestEntity (createdAt DESC);
-- GO

-- Index for finding records by creator/updater
-- CREATE INDEX IX_TestEntity_CreatedBy ON test.TestEntity (createdBy);
-- GO
