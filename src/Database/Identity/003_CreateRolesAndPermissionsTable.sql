-- =============================================
-- Script: Create Roles and RolePermissions Tables
-- Module: Identity
-- Purpose: Role-based authorization with fine-grained permissions
-- Dependencies: 001_CreateAccountsTable.sql
-- =============================================

USE HrmDb
GO

-- =============================================
-- Create Roles Table
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Roles' AND schema_id = SCHEMA_ID('Identity'))
BEGIN
    CREATE TABLE [Identity].Roles
    (
        Id                  UNIQUEIDENTIFIER    NOT NULL,
        Name                NVARCHAR(100)       NOT NULL,
        Description         NVARCHAR(500)       NULL,
        IsSystemRole        BIT                 NOT NULL DEFAULT 0,

        -- Company scope: NULL = global role, GUID = company-specific role
        CompanyId           UNIQUEIDENTIFIER    NULL,

        -- Audit Fields
        CreatedAtUtc        DATETIME2(7)        NOT NULL,
        ModifiedAtUtc       DATETIME2(7)        NULL,
        CreatedById         UNIQUEIDENTIFIER    NULL,
        ModifiedById        UNIQUEIDENTIFIER    NULL,

        -- Soft Delete
        IsDeleted           BIT                 NOT NULL DEFAULT 0,
        DeletedAtUtc        DATETIME2(7)        NULL,

        CONSTRAINT PK_Identity_Roles PRIMARY KEY CLUSTERED (Id)
    )

    PRINT 'Table [Identity].Roles created successfully'
END
ELSE
BEGIN
    PRINT 'Table [Identity].Roles already exists'
END
GO

-- Unique index on (Name, CompanyId) - role name unique within same company scope
-- SQL Server treats NULL as a value for UNIQUE, so global roles have unique names
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Identity_Roles_Name_CompanyId' AND object_id = OBJECT_ID('[Identity].Roles'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_Identity_Roles_Name_CompanyId
    ON [Identity].Roles (Name, CompanyId)
    WHERE IsDeleted = 0

    PRINT 'Index IX_Identity_Roles_Name_CompanyId created'
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Identity_Roles_IsSystemRole' AND object_id = OBJECT_ID('[Identity].Roles'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Identity_Roles_IsSystemRole
    ON [Identity].Roles (IsSystemRole)
    WHERE IsDeleted = 0

    PRINT 'Index IX_Identity_Roles_IsSystemRole created'
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Identity_Roles_CompanyId' AND object_id = OBJECT_ID('[Identity].Roles'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Identity_Roles_CompanyId
    ON [Identity].Roles (CompanyId)
    WHERE IsDeleted = 0
    INCLUDE (Name, IsSystemRole)

    PRINT 'Index IX_Identity_Roles_CompanyId created'
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Identity_Roles_IsDeleted' AND object_id = OBJECT_ID('[Identity].Roles'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Identity_Roles_IsDeleted
    ON [Identity].Roles (IsDeleted)
    INCLUDE (Name, IsSystemRole)

    PRINT 'Index IX_Identity_Roles_IsDeleted created'
END
GO

-- =============================================
-- Create RolePermissions Table (Owned Entity)
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RolePermissions' AND schema_id = SCHEMA_ID('Identity'))
BEGIN
    CREATE TABLE [Identity].RolePermissions
    (
        Id                  INT                 IDENTITY(1,1) NOT NULL,
        RoleId              UNIQUEIDENTIFIER    NOT NULL,

        -- Permission: Module.Entity.Action
        Module              NVARCHAR(50)        NOT NULL,
        Entity              NVARCHAR(50)        NOT NULL,
        Action              NVARCHAR(50)        NOT NULL,

        -- Scope level: 0=Global, 1=Company, 2=Department, 3=Position, 4=Self
        Scope               INT                 NULL,

        CONSTRAINT PK_Identity_RolePermissions PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Identity_RolePermissions_Roles FOREIGN KEY (RoleId)
            REFERENCES [Identity].Roles (Id)
            ON DELETE CASCADE,
        CONSTRAINT UQ_Identity_RolePermissions_Unique UNIQUE (RoleId, Module, Entity, Action, Scope)
    )

    PRINT 'Table [Identity].RolePermissions created successfully'
END
ELSE
BEGIN
    PRINT 'Table [Identity].RolePermissions already exists'
END
GO

-- RolePermissions Indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Identity_RolePermissions_RoleId' AND object_id = OBJECT_ID('[Identity].RolePermissions'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Identity_RolePermissions_RoleId
    ON [Identity].RolePermissions (RoleId)
    INCLUDE (Module, Entity, Action, Scope)

    PRINT 'Index IX_Identity_RolePermissions_RoleId created'
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Identity_RolePermissions_Permission' AND object_id = OBJECT_ID('[Identity].RolePermissions'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Identity_RolePermissions_Permission
    ON [Identity].RolePermissions (Module, Entity, Action)
    INCLUDE (RoleId, Scope)

    PRINT 'Index IX_Identity_RolePermissions_Permission created'
END
GO

-- Extended properties
EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Scope level (ScopeLevel enum): 0=Global, 1=Company, 2=Department, 3=Position, 4=Self',
    @level0type = N'SCHEMA', @level0name = N'Identity',
    @level1type = N'TABLE', @level1name = N'RolePermissions',
    @level2type = N'COLUMN', @level2name = N'Scope'
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Optional company ID for company-scoped roles. NULL = global role, GUID = company-specific role.',
    @level0type = N'SCHEMA', @level0name = N'Identity',
    @level1type = N'TABLE', @level1name = N'Roles',
    @level2type = N'COLUMN', @level2name = N'CompanyId'
GO

PRINT 'Script 003_CreateRolesAndPermissionsTable.sql completed'
GO
