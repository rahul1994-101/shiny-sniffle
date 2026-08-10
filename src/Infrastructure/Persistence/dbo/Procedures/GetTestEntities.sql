-- =====================================================
-- PROCEDURE: GetTestEntities
-- =====================================================
-- This procedure retrieves test records with pagination and filtering capabilities.
-- It supports filtering by name.
-- 
-- Parameters:
-- - @Page: Page number (1-based, default: 1)
-- - @PageSize: Number of records per page (default: 10)
-- - @NameFilter: Filter by test name (partial match, case-insensitive)
-- 
-- Returns: Result set with test records matching the criteria
-- =====================================================
GO

CREATE PROCEDURE dbo.GetTestEntities
    @Page INT = 1,
    @PageSize INT = 10,
    @NameFilter NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Offset INT;
    
    -- Calculate offset for pagination
    SET @Offset = (@Page - 1) * @PageSize;
    
    -- Return filtered and paginated results
    SELECT 
        t.id,
        t.name,
        t.isActive,
        t.isDeleted,
        t.createdAt,
        t.createdBy,
        t.updatedAt,
        t.updatedBy
    FROM dbo.TestEntity t
    WHERE 
        -- Only return active, non-deleted records
        t.isActive = 1 AND t.isDeleted = 0
        -- Name filter (case-insensitive partial match)
        AND (@NameFilter IS NULL OR t.name LIKE '%' + @NameFilter + '%')
    ORDER BY t.createdAt DESC  -- Most recently created first
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO

-- =====================================================
-- EXECUTION CODE
-- =====================================================
-- Uncomment the following lines to test the procedure:
-- 
-- -- Test basic functionality (get first page)
-- EXEC dbo.GetTestEntities;
-- 
-- -- Test with filters
-- EXEC dbo.GetTestEntities @Page = 1, @PageSize = 5, @NameFilter = 'Test';
-- 
-- -- Test pagination
-- EXEC dbo.GetTestEntities @Page = 2, @PageSize = 2, @NameFilter = NULL;

