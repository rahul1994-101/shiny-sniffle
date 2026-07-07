-- =====================================================
-- PROCEDURE: DeleteTestEntity
-- =====================================================
-- This procedure performs a soft delete on a test record by setting
-- the isDeleted flag to 1. The record is not physically removed
-- from the database to maintain audit trails.
-- 
-- Parameters:
-- - @Id: UNIQUEIDENTIFIER of the test record to delete (required)
-- - @UpdatedBy: UNIQUEIDENTIFIER of user deleting the record (default: system)
-- 
-- Business Rules:
-- - Only active, non-deleted records can be deleted
-- - Records are soft-deleted (isDeleted = 1), not hard-deleted
-- - Audit fields are updated to track who and when the deletion occurred
-- =====================================================
GO

CREATE PROCEDURE dbo.DeleteTestEntity
    @Id UNIQUEIDENTIFIER,
    @UpdatedBy UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000000'
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @RecordExists BIT;
    
    -- Validate input parameters
    IF @Id IS NULL
    BEGIN
        THROW 50000, 'Test record ID cannot be null', 1;
        RETURN;
    END;
    
    -- Check if record exists and is not already deleted
    SELECT @RecordExists = CASE WHEN EXISTS(
        SELECT 1 
        FROM dbo.TestEntity 
        WHERE id = @Id 
        AND isDeleted = 0
    ) THEN 1 ELSE 0 END;
    
    IF @RecordExists = 0
    BEGIN
        THROW 50000, 'Test record not found or already deleted', 1;
        RETURN;
    END;
    
    -- Perform soft delete
    UPDATE dbo.TestEntity
    SET 
        isDeleted = 1,
        isActive = 0,
        updatedBy = @UpdatedBy,
        updatedAt = GETUTCDATE()
    WHERE id = @Id;
    
    -- Log successful deletion
    PRINT 'Test record deleted successfully';
END;
GO

-- =====================================================
-- EXECUTION CODE
-- =====================================================
-- Uncomment the following lines to test the procedure:
-- 
-- -- Test successful deletion
-- EXEC dbo.DeleteTestEntity
--     @Id = '550e8400-e29b-41d4-a716-446655440001',
--     @UpdatedBy = '00000000-0000-0000-0000-000000000001';
-- 
-- -- Test validation (should fail - record not found)
-- EXEC dbo.DeleteTestEntity
--     @Id = '00000000-0000-0000-0000-000000000000';
-- 
-- -- Test validation (should fail - NULL ID)
-- EXEC dbo.DeleteTestEntity
--     @Id = NULL;

