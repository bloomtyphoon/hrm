-- =============================================
-- Script: Add TenantId to Roles
-- Module: Identity
-- Purpose: Add TenantId column and update unique index for multi-tenancy
-- Dependencies: Organization/005_CreateTenantsTable.sql, 010_AddTenantIdToAccounts.sql
-- =============================================

-- Step 1: Add TenantId column (nullable first)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[Identity].[Roles]') AND name = 'TenantId'
)
BEGIN
    ALTER TABLE [Identity].[Roles]
        ADD TenantId UNIQUEIDENTIFIER NULL

    PRINT 'Column TenantId added to [Identity].[Roles]'
END
ELSE
BEGIN
    PRINT 'Column TenantId already exists in [Identity].[Roles]'
END
GO

-- Step 2: Assign existing roles to System Tenant
DECLARE @SystemTenantId UNIQUEIDENTIFIER = 'FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF'

UPDATE [Identity].[Roles]
SET TenantId = @SystemTenantId
WHERE TenantId IS NULL
GO

-- Step 3: Make column NOT NULL
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[Identity].[Roles]')
      AND name = 'TenantId'
      AND is_nullable = 1
)
BEGIN
    ALTER TABLE [Identity].[Roles]
        ALTER COLUMN TenantId UNIQUEIDENTIFIER NOT NULL

    PRINT 'Column TenantId set to NOT NULL in [Identity].[Roles]'
END
GO

-- Step 4: Drop old unique index on (Name, CompanyId) — replaced by (TenantId, Name, CompanyId)
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Roles_Name_CompanyId' AND object_id = OBJECT_ID('[Identity].[Roles]'))
BEGIN
    DROP INDEX IX_Roles_Name_CompanyId ON [Identity].[Roles]
    PRINT 'Old index IX_Roles_Name_CompanyId dropped'
END
GO

-- Step 5: Add new unique index on (TenantId, Name, CompanyId)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Roles_TenantId_Name_CompanyId' AND object_id = OBJECT_ID('[Identity].[Roles]'))
BEGIN
    CREATE UNIQUE INDEX IX_Roles_TenantId_Name_CompanyId
        ON [Identity].[Roles] (TenantId, Name, CompanyId)
        WHERE IsDeleted = 0
    PRINT 'Index IX_Roles_TenantId_Name_CompanyId created'
END
GO

-- Step 6: Add non-unique index on TenantId
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Roles_TenantId' AND object_id = OBJECT_ID('[Identity].[Roles]'))
BEGIN
    CREATE INDEX IX_Roles_TenantId ON [Identity].[Roles] (TenantId)
    PRINT 'Index IX_Roles_TenantId created'
END
GO
