-- =============================================
-- Script: Add TenantId to EmployeeProfiles
-- Module: Identity
-- Purpose: Add TenantId column for multi-tenant row-level isolation
-- Dependencies: 010_AddTenantIdToAccounts.sql
-- =============================================

-- Step 1: Add TenantId column (nullable first)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[Identity].[EmployeeProfiles]') AND name = 'TenantId'
)
BEGIN
    ALTER TABLE [Identity].[EmployeeProfiles]
        ADD TenantId UNIQUEIDENTIFIER NULL

    PRINT 'Column TenantId added to [Identity].[EmployeeProfiles]'
END
ELSE
BEGIN
    PRINT 'Column TenantId already exists in [Identity].[EmployeeProfiles]'
END
GO

-- Step 2: Assign existing rows to System Tenant
DECLARE @SystemTenantId UNIQUEIDENTIFIER = 'FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF'

UPDATE [Identity].[EmployeeProfiles]
SET TenantId = @SystemTenantId
WHERE TenantId IS NULL
GO

-- Step 3: Make column NOT NULL
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[Identity].[EmployeeProfiles]')
      AND name = 'TenantId'
      AND is_nullable = 1
)
BEGIN
    ALTER TABLE [Identity].[EmployeeProfiles]
        ALTER COLUMN TenantId UNIQUEIDENTIFIER NOT NULL

    PRINT 'Column TenantId set to NOT NULL in [Identity].[EmployeeProfiles]'
END
GO

-- Step 4: Add index
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EmployeeProfiles_TenantId' AND object_id = OBJECT_ID('[Identity].[EmployeeProfiles]'))
BEGIN
    CREATE INDEX IX_EmployeeProfiles_TenantId ON [Identity].[EmployeeProfiles] (TenantId)
    PRINT 'Index IX_EmployeeProfiles_TenantId created'
END
GO
