-- =====================================================
-- FUNCTION TEMPLATE: Multi-Statement Table-Valued Function
-- =====================================================
-- This is a template for creating multi-statement table-valued functions in SQL Server.
-- Multi-statement table-valued functions can contain complex logic, variables, and multiple statements.
-- They return a table variable that must be explicitly populated.
-- 
-- Note: These functions can have performance implications. Consider using stored procedures
-- for complex data retrieval operations instead.
-- 
-- Usage: Copy this template and modify as needed
-- =====================================================

CREATE OR ALTER FUNCTION dbo.MultiStatementTableValuedFunctionTemplate(
    @Parameter1 INT = 1,
    @Parameter2 NVARCHAR(100) = NULL
)
RETURNS @result TABLE (
    id UNIQUEIDENTIFIER,
    name NVARCHAR(100),
    isActive BIT,
    createdAt DATETIME2
)
AS
BEGIN
    DECLARE @Variable INT;
    
    -- Set variable values
    SET @Variable = @Parameter1;
    
    -- Populate result table
    INSERT INTO @result
    SELECT 
        t.id,
        t.name,
        t.isActive,
        t.createdAt
    FROM dbo.TestEntity t
    WHERE 
        t.isActive = 1
        AND (@Parameter1 IS NULL OR t.id = CAST(@Parameter1 AS UNIQUEIDENTIFIER))
        AND (@Parameter2 IS NULL OR t.name LIKE '%' + @Parameter2 + '%');
    
    RETURN;
END;

-- =====================================================
-- EXECUTION EXAMPLE
-- =====================================================
-- SELECT * FROM dbo.MultiStatementTableValuedFunctionTemplate(1, 'test');

