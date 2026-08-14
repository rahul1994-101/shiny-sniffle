-- =====================================================
-- VIEW: TestEntityView
-- =====================================================
-- This view provides a comprehensive overview of test records.
-- 
-- Business Purpose:
-- - Dashboard displays showing test record activity
-- - Test record analysis and reporting
-- 
-- Columns:
-- - All test entity fields
-- =====================================================
GO

CREATE VIEW test.TestEntityView
AS
SELECT 
    -- Test record identification
    t.id,
    t.name,
    
    -- Status fields
    t.isActive,
    t.isDeleted,
    
    -- Audit fields
    t.createdAt,
    t.createdBy,
    t.updatedAt,
    t.updatedBy
FROM test.TestEntity t
WHERE t.isActive = 1 AND t.isDeleted = 0;
GO

-- =====================================================
-- EXECUTION CODE
-- =====================================================
-- Uncomment the following lines to test the view:
-- 
-- -- Test basic functionality
-- SELECT * FROM test.TestEntityView;
-- 
-- -- Test filtering by name
-- SELECT * FROM test.TestEntityView 
-- WHERE name LIKE '%Test%';
