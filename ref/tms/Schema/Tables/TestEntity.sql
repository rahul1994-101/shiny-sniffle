-- =====================================================
-- TEST ENTITY TABLE
-- =====================================================
-- This table stores test data for TenantDB.
-- 
-- Business Rules:
-- - Each record has a unique name
-- - All records include audit fields for tracking changes
-- =====================================================

CREATE TABLE dbo.TestEntity (
    -- Primary key with auto-generated sequential UUID
    id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),

    -- Data fields
    name NVARCHAR(100) NOT NULL,                    -- Test name (must be unique)

    -- Status and lifecycle management
    isActive BIT DEFAULT 1,                 -- Whether the record is active
    isDeleted BIT DEFAULT 0,              -- Soft delete flag for data retention

    -- Audit fields for tracking changes
    createdBy UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    createdAt DATETIME2 DEFAULT SYSUTCDATETIME(),
    updatedBy UNIQUEIDENTIFIER DEFAULT '00000000-0000-0000-0000-000000000000',
    updatedAt DATETIME2 DEFAULT SYSUTCDATETIME()
);

-- =====================================================
-- INDEXES FOR TEST ENTITY TABLE
-- =====================================================

-- Index for quick lookups by name (most common query)
CREATE INDEX IX_TestEntity_Name ON dbo.TestEntity (name);

-- Index for filtering by active status (most common business query)
CREATE INDEX IX_TestEntity_IsActive ON dbo.TestEntity (isActive) WHERE isActive = 1;

-- Index for filtering by deletion status (soft delete queries)
CREATE INDEX IX_TestEntity_IsDeleted ON dbo.TestEntity (isDeleted) WHERE isDeleted = 0;

-- Composite index for active, non-deleted records (business queries)
CREATE INDEX IX_TestEntity_IsActive_IsDeleted ON dbo.TestEntity (isActive, isDeleted) WHERE isActive = 1 AND isDeleted = 0;

-- Index for audit queries (finding records by creation/update time)
CREATE INDEX IX_TestEntity_CreatedAt ON dbo.TestEntity (createdAt DESC);

-- Index for finding records by creator/updater
CREATE INDEX IX_TestEntity_CreatedBy ON dbo.TestEntity (createdBy);
