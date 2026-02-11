-- =============================================
-- Script: Add CompanyId to Roles table for company-scoped roles
-- Module: Identity
-- Purpose: Allow roles to be specific to a company (multi-company support)
-- Dependencies: 003_CreateRolesAndPermissionsTable.sql
-- =============================================

USE HrmDb
GO

-- =============================================
-- Add CompanyId column to Roles table
-- NULL = global role (available to all companies)
-- GUID = company-specific role (only for employees in that company)
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[Identity].Roles') AND name = 'CompanyId')
BEGIN
    ALTER TABLE [Identity].Roles
    ADD CompanyId UNIQUEIDENTIFIER NULL

    PRINT 'Column CompanyId added to [Identity].Roles'
END
ELSE
BEGIN
    PRINT 'Column CompanyId already exists on [Identity].Roles'
END
GO

-- =============================================
-- Drop old unique index on Name only
-- =============================================
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Identity_Roles_Name' AND object_id = OBJECT_ID('[Identity].Roles'))
BEGIN
    ALTER TABLE [Identity].Roles
    DROP CONSTRAINT UQ_Identity_Roles_Name

    PRINT 'Constraint UQ_Identity_Roles_Name dropped'
END
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Roles_Name' AND object_id = OBJECT_ID('[Identity].Roles'))
BEGIN
    DROP INDEX IX_Roles_Name ON [Identity].Roles

    PRINT 'Index IX_Roles_Name dropped'
END
GO

-- =============================================
-- Create new unique index on (Name, CompanyId)
-- SQL Server treats NULL as a value for UNIQUE constraints,
-- so (Name='HR Manager', CompanyId=NULL) can only appear once.
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Roles_Name_CompanyId' AND object_id = OBJECT_ID('[Identity].Roles'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_Roles_Name_CompanyId
    ON [Identity].Roles (Name, CompanyId)
    WHERE IsDeleted = 0

    PRINT 'Index IX_Roles_Name_CompanyId created'
END
GO

-- =============================================
-- Create index on CompanyId for filtering
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Roles_CompanyId' AND object_id = OBJECT_ID('[Identity].Roles'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Roles_CompanyId
    ON [Identity].Roles (CompanyId)
    WHERE IsDeleted = 0
    INCLUDE (Name, IsSystemRole)

    PRINT 'Index IX_Roles_CompanyId created'
END
GO

-- Extended properties
EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Optional company ID for company-scoped roles. NULL = global role, GUID = company-specific role.',
    @level0type = N'SCHEMA', @level0name = N'Identity',
    @level1type = N'TABLE', @level1name = N'Roles',
    @level2type = N'COLUMN', @level2name = N'CompanyId'
GO

PRINT 'Script 009_AddCompanyIdToRoles.sql completed'
GO
