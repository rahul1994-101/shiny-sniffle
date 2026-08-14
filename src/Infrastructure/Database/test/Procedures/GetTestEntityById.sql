-- =====================================================
-- PROCEDURE: GetTestEntityById
-- =====================================================
-- This procedure retrieves a single test record by its ID.
-- 
-- Parameters:
-- - @Id: UNIQUEIDENTIFIER of the test record to retrieve
-- 
-- Returns: Result set with single test record matching the ID, or empty if not found
-- =====================================================
GO

CREATE PROCEDURE test.GetTestEntityById
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        t.id,
        t.name,
        t.isActive,
        t.isDeleted,
        t.createdAt,
        t.createdBy,
        t.updatedAt,
        t.updatedBy
    FROM test.TestEntity t
    WHERE 
        -- Match the provided ID
        t.id = @Id
        -- Only return active, non-deleted records
        AND t.isActive = 1 
        AND t.isDeleted = 0;
END;
GO

-- =====================================================
-- EXECUTION CODE
-- =====================================================
-- Uncomment the following lines to test the procedure:
-- 
-- -- Test basic functionality (get by ID)
-- EXEC test.GetTestEntityById @Id = '550e8400-e29b-41d4-a716-446655440001';
-- 
-- -- Test with non-existent ID (should return empty)
-- EXEC test.GetTestEntityById @Id = '00000000-0000-0000-0000-000000000000';
