-- =====================================================
-- CHAT SCHEMA (per-user conversation threads and messages)
-- =====================================================
-- Run once on existing databases. Safe to re-run.
-- Apply after dbo.User; before Tables/*.sql in this folder
-- =====================================================
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'chat')
BEGIN
    EXEC(N'CREATE SCHEMA [chat]');
END
GO
