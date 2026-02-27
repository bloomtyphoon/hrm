-- =============================================
-- Script: Add TenantId to Companies
-- Module: Organization
-- Purpose: Add TenantId column for multi-tenant row-level isolation
-- Dependencies: 005_CreateTenantsTable.sql (Tenants table must exist)
-- =============================================

-- Step 1: Add TenantId column (nullable first to allow update of existing rows)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[Organization].[Companies]') AND name = 'TenantId'
)
BEGIN
    ALTER TABLE [Organization].[Companies]
        ADD TenantId UNIQUEIDENTIFIER NULL

    PRINT 'Column TenantId added to [Organization].[Companies]'
END
ELSE
BEGIN
    PRINT 'Column TenantId already exists in [Organization].[Companies]'
END
GO

-- Step 2: Assign existing rows to System Tenant
-- Existing data predates multi-tenancy; assign to System Tenant by convention
DECLARE @SystemTenantId UNIQUEIDENTIFIER = 'FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF'

UPDATE [Organization].[Companies]
SET TenantId = @SystemTenantId
WHERE TenantId IS NULL
GO

-- Step 3: Make column NOT NULL now that all rows have a value
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[Organization].[Companies]')
      AND name = 'TenantId'
      AND is_nullable = 1
)
BEGIN
    ALTER TABLE [Organization].[Companies]
        ALTER COLUMN TenantId UNIQUEIDENTIFIER NOT NULL

    PRINT 'Column TenantId set to NOT NULL in [Organization].[Companies]'
END
GO

-- Step 4: Add index
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Companies_TenantId' AND object_id = OBJECT_ID('[Organization].[Companies]'))
BEGIN
    CREATE INDEX IX_Companies_TenantId ON [Organization].[Companies] (TenantId)
    PRINT 'Index IX_Companies_TenantId created'
END
GO
