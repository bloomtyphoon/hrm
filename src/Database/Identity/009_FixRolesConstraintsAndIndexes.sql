-- =============================================
-- Script: Fix Roles Constraints and Indexes
-- Module: Identity
-- Purpose: Fix unique indexes for NULL CompanyId, add CHECK constraints, add performance indexes
-- Dependencies: 003_CreateRolesAndPermissionsTable.sql
-- =============================================

USE HrmDb
GO

-- =============================================
-- #2 CRITICAL: Replace single unique index with 2 filtered unique indexes
-- Problem: SQL Server treats each NULL as distinct in UNIQUE indexes,
--          so (Name, CompanyId) WHERE IsDeleted=0 allows duplicate global role names.
-- Solution: Two filtered indexes - one for global roles, one for company-scoped roles.
-- =============================================

-- Drop the old incorrect index
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Identity_Roles_Name_CompanyId' AND object_id = OBJECT_ID('[Identity].Roles'))
BEGIN
    DROP INDEX IX_Identity_Roles_Name_CompanyId ON [Identity].Roles
    PRINT 'Dropped old index IX_Identity_Roles_Name_CompanyId'
END
GO

-- Global roles: unique Name where CompanyId IS NULL (no duplicates among global roles)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Identity_Roles_Name_Global' AND object_id = OBJECT_ID('[Identity].Roles'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_Identity_Roles_Name_Global
    ON [Identity].Roles (Name)
    WHERE CompanyId IS NULL AND IsDeleted = 0

    PRINT 'Index UX_Identity_Roles_Name_Global created'
END
GO

-- Company-scoped roles: unique (Name, CompanyId) where CompanyId IS NOT NULL
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Identity_Roles_Name_CompanyId' AND object_id = OBJECT_ID('[Identity].Roles'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_Identity_Roles_Name_CompanyId
    ON [Identity].Roles (Name, CompanyId)
    WHERE CompanyId IS NOT NULL AND IsDeleted = 0

    PRINT 'Index UX_Identity_Roles_Name_CompanyId created'
END
GO

-- =============================================
-- #3 HIGH: CHECK constraint - IsSystemRole=1 requires CompanyId IS NULL
-- Business rule: System roles are always global (no company scope)
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Identity_Roles_SystemRole_GlobalOnly' AND parent_object_id = OBJECT_ID('[Identity].Roles'))
BEGIN
    ALTER TABLE [Identity].Roles
    ADD CONSTRAINT CK_Identity_Roles_SystemRole_GlobalOnly
    CHECK (IsSystemRole = 0 OR CompanyId IS NULL)

    PRINT 'CHECK constraint CK_Identity_Roles_SystemRole_GlobalOnly created'
END
GO

-- =============================================
-- #5 MEDIUM: Performance indexes for common role queries
-- =============================================

-- Covering index for "get roles by company" queries (most common query pattern)
-- Includes global roles (CompanyId IS NULL) + company-specific roles
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Identity_Roles_CompanyId_Active' AND object_id = OBJECT_ID('[Identity].Roles'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Identity_Roles_CompanyId_Active
    ON [Identity].Roles (CompanyId)
    WHERE IsDeleted = 0
    INCLUDE (Name, Description, IsSystemRole, CreatedAtUtc, ModifiedAtUtc)

    PRINT 'Index IX_Identity_Roles_CompanyId_Active created'
END
GO

-- =============================================
-- #6 LOW: CHECK constraint for RolePermissions.Scope
-- Scope values: 0=Global, 1=Company, 2=Department, 3=Position, 4=Self, NULL=unset
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Identity_RolePermissions_Scope' AND parent_object_id = OBJECT_ID('[Identity].RolePermissions'))
BEGIN
    ALTER TABLE [Identity].RolePermissions
    ADD CONSTRAINT CK_Identity_RolePermissions_Scope
    CHECK (Scope IS NULL OR (Scope >= 0 AND Scope <= 4))

    PRINT 'CHECK constraint CK_Identity_RolePermissions_Scope created'
END
GO

PRINT 'Script 009_FixRolesConstraintsAndIndexes.sql completed'
GO
