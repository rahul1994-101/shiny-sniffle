-- =====================================================
-- MIGRATION: workflow schema (rename from automation shell)
-- =====================================================
-- Safe if 20260810 already created [automation] with no tables.
-- =====================================================

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'workflow')
BEGIN
    EXEC(N'CREATE SCHEMA [workflow]');
END
GO

IF EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'automation')
   AND NOT EXISTS (
       SELECT 1
       FROM sys.tables t
       WHERE t.schema_id = SCHEMA_ID(N'automation'))
BEGIN
    DROP SCHEMA [automation];
END
GO
