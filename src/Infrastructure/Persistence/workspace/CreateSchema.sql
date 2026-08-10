-- =====================================================
-- WORKSPACE SCHEMA (user-owned internal reference data)
-- =====================================================
-- Run once on existing databases. Safe to re-run.
-- Apply before workspace/Tables/*.sql (e.g. Contact).
-- =====================================================
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'workspace')
BEGIN
    EXEC(N'CREATE SCHEMA [workspace]');
END
GO
