-- =============================================
-- Script: Add TenantId to Employees
-- Module: Personnel
-- Purpose: Add TenantId column for multi-tenant row-level isolation
-- Dependencies: Organization/005_CreateTenantsTable.sql
-- =============================================

-- Step 1: Add TenantId column (nullable first)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[Personnel].[Employees]') AND name = 'TenantId'
)
BEGIN
    ALTER TABLE [Personnel].[Employees]
        ADD TenantId UNIQUEIDENTIFIER NULL

    PRINT 'Column TenantId added to [Personnel].[Employees]'
END
ELSE
BEGIN
    PRINT 'Column TenantId already exists in [Personnel].[Employees]'
END
GO

-- Step 2: Assign existing rows to System Tenant
DECLARE @SystemTenantId UNIQUEIDENTIFIER = 'FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF'

UPDATE [Personnel].[Employees]
SET TenantId = @SystemTenantId
WHERE TenantId IS NULL
GO

-- Step 3: Make column NOT NULL
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[Personnel].[Employees]')
      AND name = 'TenantId'
      AND is_nullable = 1
)
BEGIN
    ALTER TABLE [Personnel].[Employees]
        ALTER COLUMN TenantId UNIQUEIDENTIFIER NOT NULL

    PRINT 'Column TenantId set to NOT NULL in [Personnel].[Employees]'
END
GO

-- Step 4: Add index
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Employees_TenantId' AND object_id = OBJECT_ID('[Personnel].[Employees]'))
BEGIN
    CREATE INDEX IX_Employees_TenantId ON [Personnel].[Employees] (TenantId)
    PRINT 'Index IX_Employees_TenantId created'
END
GO
