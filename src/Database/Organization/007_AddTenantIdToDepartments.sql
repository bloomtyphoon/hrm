-- =============================================
-- Script: Add TenantId to Departments
-- Module: Organization
-- Purpose: Add TenantId column for multi-tenant row-level isolation
-- Dependencies: 005_CreateTenantsTable.sql, 006_AddTenantIdToCompanies.sql
-- =============================================

-- Step 1: Add TenantId column (nullable first)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[Organization].[Departments]') AND name = 'TenantId'
)
BEGIN
    ALTER TABLE [Organization].[Departments]
        ADD TenantId UNIQUEIDENTIFIER NULL

    PRINT 'Column TenantId added to [Organization].[Departments]'
END
ELSE
BEGIN
    PRINT 'Column TenantId already exists in [Organization].[Departments]'
END
GO

-- Step 2: Assign existing rows to System Tenant
DECLARE @SystemTenantId UNIQUEIDENTIFIER = 'FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF'

UPDATE [Organization].[Departments]
SET TenantId = @SystemTenantId
WHERE TenantId IS NULL
GO

-- Step 3: Make column NOT NULL
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[Organization].[Departments]')
      AND name = 'TenantId'
      AND is_nullable = 1
)
BEGIN
    ALTER TABLE [Organization].[Departments]
        ALTER COLUMN TenantId UNIQUEIDENTIFIER NOT NULL

    PRINT 'Column TenantId set to NOT NULL in [Organization].[Departments]'
END
GO

-- Step 4: Add index
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Departments_TenantId' AND object_id = OBJECT_ID('[Organization].[Departments]'))
BEGIN
    CREATE INDEX IX_Departments_TenantId ON [Organization].[Departments] (TenantId)
    PRINT 'Index IX_Departments_TenantId created'
END
GO
