-- =====================================================
-- FUNCTION TEMPLATE: Inline Table-Valued Function
-- =====================================================
-- This is a template for creating inline table-valued functions in SQL Server.
-- Inline table-valued functions return a table and are optimized by SQL Server.
-- They can only contain a single SELECT statement.
-- 
-- Usage: Copy this template and modify as needed
-- =====================================================
GO

CREATE FUNCTION dbo.InlineTableValuedFunctionTemplate(
    @Parameter1 INT,
    @Parameter2 NVARCHAR(100) = NULL
)
RETURNS TABLE
AS
RETURN
(
    SELECT 
        t.id,
        t.name
    FROM dbo.TestEntity t
    WHERE 
        t.isActive = 1
        AND (@Parameter1 IS NULL OR t.id = @Parameter1)
        AND (@Parameter2 IS NULL OR t.name LIKE '%' + @Parameter2 + '%')
);
GO

-- =====================================================
-- EXECUTION EXAMPLE
-- =====================================================
-- SELECT * FROM dbo.InlineTableValuedFunctionTemplate(1, 'test');

