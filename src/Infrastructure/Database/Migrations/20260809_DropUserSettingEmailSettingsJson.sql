-- =====================================================
-- MIGRATION: Drop legacy UserSetting.EmailSettingsJson
-- =====================================================
-- Prerequisite: run 20260808_EmailAccount.sql (backfill into dbo.EmailAccount).
-- =====================================================
GO

IF COL_LENGTH(N'dbo.UserSetting', N'EmailSettingsJson') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[UserSetting] DROP COLUMN [EmailSettingsJson];
END
GO
