-- =====================================================
-- WORKFLOW SCHEMA (empty shell for future rules engine)
-- =====================================================
-- Run once on existing databases. Safe to re-run.
-- =====================================================
GO

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
