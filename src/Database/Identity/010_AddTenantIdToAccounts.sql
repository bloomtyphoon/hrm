-- =============================================
-- Script: Add TenantId to Accounts
-- Module: Identity
-- Purpose: Add TenantId column for multi-tenant row-level isolation
-- Dependencies: Organization/005_CreateTenantsTable.sql
-- =============================================

-- Step 1: Add TenantId column (nullable first)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[Identity].[Accounts]') AND name = 'TenantId'
)
BEGIN
    ALTER TABLE [Identity].[Accounts]
        ADD TenantId UNIQUEIDENTIFIER NULL

    PRINT 'Column TenantId added to [Identity].[Accounts]'
END
ELSE
BEGIN
    PRINT 'Column TenantId already exists in [Identity].[Accounts]'
END
GO

-- Step 2: Assign existing rows to System Tenant
-- All pre-migration accounts are treated as system accounts
DECLARE @SystemTenantId UNIQUEIDENTIFIER = 'FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF'

UPDATE [Identity].[Accounts]
SET TenantId = @SystemTenantId
WHERE TenantId IS NULL
GO

-- Step 3: Make column NOT NULL
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[Identity].[Accounts]')
      AND name = 'TenantId'
      AND is_nullable = 1
)
BEGIN
    ALTER TABLE [Identity].[Accounts]
        ALTER COLUMN TenantId UNIQUEIDENTIFIER NOT NULL

    PRINT 'Column TenantId set to NOT NULL in [Identity].[Accounts]'
END
GO

-- Step 4: Add index
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Accounts_TenantId' AND object_id = OBJECT_ID('[Identity].[Accounts]'))
BEGIN
    CREATE INDEX IX_Accounts_TenantId ON [Identity].[Accounts] (TenantId)
    PRINT 'Index IX_Accounts_TenantId created'
END
GO
