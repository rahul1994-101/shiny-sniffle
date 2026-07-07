-- =====================================================
-- MASTER DATA SEED SCRIPT
-- =====================================================
-- This script populates the database with initial reference data
-- for testing and development purposes. It uses specific GUIDs
-- to ensure consistent data across environments.
-- 
-- Note: This script is idempotent and can be run multiple times
-- without creating duplicate data.
-- =====================================================
GO

-- =====================================================
-- TEST ENTITY SEED DATA
-- =====================================================
--MERGE [dbo].[TestEntity] AS target
--USING (VALUES
--    ('550e8400-e29b-41d4-a716-446655440001', 'Test Record 01', 1, 0, '00000000-0000-0000-0000-000000000000', '00000000-0000-0000-0000-000000000000'),
--    ('550e8400-e29b-41d4-a716-446655440002', 'Test Record 02', 1, 0, '00000000-0000-0000-0000-000000000000', '00000000-0000-0000-0000-000000000000'),
--    ('550e8400-e29b-41d4-a716-446655440003', 'Test Record 03', 1, 0, '00000000-0000-0000-0000-000000000000', '00000000-0000-0000-0000-000000000000')
--) AS source (id, name, isActive, isDeleted, createdBy, updatedBy)
--ON target.id = source.id
--WHEN NOT MATCHED THEN
--    INSERT (id, name, isActive, isDeleted, createdBy, updatedBy)
--    VALUES (source.id, source.name, source.isActive, source.isDeleted, source.createdBy, source.updatedBy);
GO

-- =====================================================
-- USER SEED DATA
-- =====================================================
--MERGE [dbo].[User] AS target
--USING (VALUES
--    ('550e8400-e29b-41d4-a716-446655440001',    'John',    'Doe', 'a@gmail.com', '9876543210',                'aa',        1,         0, '00000000-0000-0000-0000-000000000000', '00000000-0000-0000-0000-000000000000')
--) AS source (id,                             firstName, lastName,         email,       mobile,            password, isActive, isDeleted,                              createdBy,                              updatedBy)
--ON target.id = source.id
--WHEN NOT MATCHED THEN
--    INSERT (id, firstName, lastName, email, mobile, password, isActive, isDeleted, createdBy, updatedBy) 
--    VALUES (source.id, source.firstName, source.lastName, source.email, source.mobile, source.password, source.isActive, source.isDeleted, source.createdBy, source.updatedBy);
GO
