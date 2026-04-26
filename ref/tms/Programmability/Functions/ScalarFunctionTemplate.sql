-- =====================================================
-- FUNCTION TEMPLATE: Scalar Function
-- =====================================================
-- This is a template for creating scalar functions in SQL Server.
-- Scalar functions return a single value.
-- 
-- Usage: Copy this template and modify as needed
-- =====================================================

CREATE OR ALTER FUNCTION dbo.ScalarFunctionTemplate(
    @Parameter1 INT,
    @Parameter2 NVARCHAR(100) = NULL
)
RETURNS INT
AS
BEGIN
    DECLARE @Result INT;
    
    -- Function logic here
    SET @Result = @Parameter1;
    
    RETURN @Result;
END;

-- =====================================================
-- EXECUTION EXAMPLE
-- =====================================================
-- SELECT dbo.ScalarFunctionTemplate(1, 'test');

