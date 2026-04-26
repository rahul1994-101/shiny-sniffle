-- =====================================================
-- PROCEDURE: UpsertTestEntity
-- =====================================================
-- This procedure creates or updates a test record with validation.
-- If the record exists (by ID), it updates it; otherwise, it inserts a new one.
-- It ensures data integrity and sets appropriate audit fields.
-- 
-- Parameters:
-- - @Name: Test name (required, max 100 characters)
-- - @Id: Test record ID (UNIQUEIDENTIFIER, if provided and exists, will update; if NULL, will insert)
-- - @IsActive: Whether the record is active (default: 1)
-- - @UpdatedBy: UNIQUEIDENTIFIER of user updating the record (default: system)
-- - @CreatedBy: UNIQUEIDENTIFIER of user creating the record (default: system, used only on insert)
-- 
-- Business Rules:
-- - Test name must be unique
-- - All fields are validated before insertion/update
-- =====================================================

CREATE OR ALTER PROCEDURE dbo.UpsertTestEntity
    @Name NVARCHAR(100),
    @Id UNIQUEIDENTIFIER = NULL,
    @IsActive BIT = 1,
    @UpdatedBy UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000000',
    @CreatedBy UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000000'
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @RecordExists BIT;
    
    -- Validate input parameters
    IF @Name IS NULL OR LTRIM(RTRIM(@Name)) = ''
    BEGIN
        THROW 50000, 'Test name cannot be null or empty', 1;
        RETURN;
    END;
    
    -- Check if record exists (if ID is provided)
    IF @Id IS NOT NULL
    BEGIN
        SELECT @RecordExists = CASE WHEN EXISTS(SELECT 1 FROM dbo.TestEntity WHERE id = @Id AND isDeleted = 0) THEN 1 ELSE 0 END;
    END
    ELSE
    BEGIN
        SET @RecordExists = 0;
    END;
    
    -- Perform upsert operation
    IF @RecordExists = 1
    BEGIN
        -- Update existing record
        -- Check for duplicate name (excluding current record)
        IF EXISTS (SELECT 1 FROM dbo.TestEntity WHERE name = @Name AND id != @Id AND isDeleted = 0)
        BEGIN
            THROW 50000, 'Test with name already exists', 1;
            RETURN;
        END;
        
        UPDATE dbo.TestEntity
        SET 
            name = @Name,
            isActive = @IsActive,
            updatedBy = @UpdatedBy,
            updatedAt = GETUTCDATE()
        WHERE id = @Id;
        
        PRINT 'Test record updated successfully';
    END
    ELSE
    BEGIN
        -- Insert new record
        -- Check for duplicate test name
        IF EXISTS (SELECT 1 FROM dbo.TestEntity WHERE name = @Name AND isDeleted = 0)
        BEGIN
            THROW 50000, 'Test with name already exists', 1;
            RETURN;
        END;
        
        INSERT INTO dbo.TestEntity (id, name, isActive, isDeleted, createdBy, updatedBy)
        VALUES (
            ISNULL(@Id, NEWID()), 
            @Name, 
            @IsActive, 
            0, 
            @CreatedBy, 
            @CreatedBy
        );
        
        PRINT 'Test record created successfully';
    END;
END;

-- =====================================================
-- EXECUTION CODE
-- =====================================================
-- Uncomment the following lines to test the procedure:
-- 
-- -- Test insert (new record)
-- EXEC dbo.UpsertTestEntity
--     @Name = 'Test Record',
--     @Id = NULL,
--     @IsActive = 1,
--     @CreatedBy = '00000000-0000-0000-0000-000000000001';
-- 
-- -- Test update (existing record)
-- EXEC dbo.UpsertTestEntity
--     @Name = 'Updated Test Record',
--     @Id = '550e8400-e29b-41d4-a716-446655440001',
--     @IsActive = 1,
--     @UpdatedBy = '00000000-0000-0000-0000-000000000002';
-- 
-- -- Test validation (should fail)
-- EXEC dbo.UpsertTestEntity
--     @Name = NULL;
-- 
-- -- Clean up test data
-- DELETE FROM dbo.TestEntity WHERE name IN ('Test Record', 'Updated Test Record');

