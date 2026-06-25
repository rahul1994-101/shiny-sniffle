-- =====================================================
-- TRIGGER: TestEntityInsertTrigger
-- =====================================================
-- This trigger fires after each test record is inserted
-- but performs no operation. It exists only for testing purposes.
-- 
-- Business Purpose:
-- - Placeholder trigger for testing
-- - No business logic or data modification
-- 
-- Trigger Event: AFTER INSERT on TestEntity
-- =====================================================
GO

CREATE TRIGGER dbo.TestEntityInsertTrigger
ON dbo.TestEntity
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- No operation - this trigger does nothing
    -- It exists only for testing purposes
    -- Access inserted rows via INSERTED table if needed
    -- DECLARE @id UNIQUEIDENTIFIER;
    -- SELECT @id = id FROM INSERTED;
    
    -- No action performed
    RETURN;
END;
GO

-- =====================================================
-- EXECUTION CODE
-- =====================================================
-- Uncomment the following lines to test the trigger:
-- 
-- -- Test trigger by creating a test record
-- INSERT INTO dbo.TestEntity (name)
-- VALUES ('Test Record');
-- 
-- -- Verify the record was created (trigger does nothing, so record should exist)
-- SELECT * FROM dbo.TestEntity WHERE name = 'Test Record';
-- 
-- -- Clean up test data
-- DELETE FROM dbo.TestEntity WHERE name = 'Test Record';

