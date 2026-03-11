-- =============================================
-- Script: Create Roles and RolePermissions Tables
-- Module: Identity
-- Purpose: Role-based authorization with fine-grained permissions
-- Dependencies: 001_CreateAccountsTable.sql
-- =============================================

-- =============================================
-- Create Roles Table
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Roles' AND schema_id = SCHEMA_ID('Identity'))
BEGIN
    CREATE TABLE [Identity].Roles
    (
        Id                  UNIQUEIDENTIFIER    NOT NULL,

        -- Multi-Tenant: references Organization.Tenants (soft reference, no FK for cross-schema independence)
        -- SystemTenantId = FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF
        TenantId            UNIQUEIDENTIFIER    NOT NULL,

        Name                NVARCHAR(100)       NOT NULL,
        Description         NVARCHAR(500)       NULL,
        IsSystemRole        BIT                 NOT NULL DEFAULT 0,

        -- Company scope: NULL = tenant-global role, GUID = company-specific role
        CompanyId           UNIQUEIDENTIFIER    NULL,

        -- Audit Fields
        CreatedAtUtc        DATETIME2(7)        NOT NULL,
        ModifiedAtUtc       DATETIME2(7)        NULL,
        CreatedById         UNIQUEIDENTIFIER    NULL,
        ModifiedById        UNIQUEIDENTIFIER    NULL,

        -- Soft Delete
        IsDeleted           BIT                 NOT NULL DEFAULT 0,
        DeletedAtUtc        DATETIME2(7)        NULL,

        CONSTRAINT PK_Identity_Roles PRIMARY KEY CLUSTERED (Id),
        -- System roles must be global (no company scope)
        CONSTRAINT CK_Identity_Roles_SystemRole_GlobalOnly CHECK (IsSystemRole = 0 OR CompanyId IS NULL)
    )

    PRINT 'Table [Identity].Roles created successfully'
END
ELSE
BEGIN
    PRINT 'Table [Identity].Roles already exists'
END
GO

-- =============================================
-- Roles Indexes
-- =============================================

-- TenantId index (for multi-tenant query filter)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Identity_Roles_TenantId' AND object_id = OBJECT_ID('[Identity].Roles'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Identity_Roles_TenantId
    ON [Identity].Roles (TenantId)
    WHERE IsDeleted = 0

    PRINT 'Index IX_Identity_Roles_TenantId created'
END
GO

-- Global roles within tenant: unique Name where CompanyId IS NULL
-- (prevents duplicate global role names within the same tenant)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Identity_Roles_TenantId_Name_Global' AND object_id = OBJECT_ID('[Identity].Roles'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_Identity_Roles_TenantId_Name_Global
    ON [Identity].Roles (TenantId, Name)
    WHERE CompanyId IS NULL AND IsDeleted = 0

    PRINT 'Index UX_Identity_Roles_TenantId_Name_Global created'
END
GO

-- Company-scoped roles within tenant: unique (TenantId, Name, CompanyId) where CompanyId IS NOT NULL
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Identity_Roles_TenantId_Name_CompanyId' AND object_id = OBJECT_ID('[Identity].Roles'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_Identity_Roles_TenantId_Name_CompanyId
    ON [Identity].Roles (TenantId, Name, CompanyId)
    WHERE CompanyId IS NOT NULL AND IsDeleted = 0

    PRINT 'Index UX_Identity_Roles_TenantId_Name_CompanyId created'
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

-- Covering index for "get roles by company" queries (most common query pattern)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Identity_Roles_CompanyId_Active' AND object_id = OBJECT_ID('[Identity].Roles'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Identity_Roles_CompanyId_Active
    ON [Identity].Roles (CompanyId)
    INCLUDE (TenantId, Name, Description, IsSystemRole, CreatedAtUtc, ModifiedAtUtc)
    WHERE IsDeleted = 0

    PRINT 'Index IX_Identity_Roles_CompanyId_Active created'
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

        -- Scope level (matches DataScopeLevel enum):
        --   0=None (explicit deny), 1=Self, 2=EmployeeSet, 3=Position, 4=Department, 5=Company, 6=Global
        -- NULL = permission exists but scope is not constrained (legacy / unchecked)
        Scope               INT                 NULL,

        CONSTRAINT PK_Identity_RolePermissions PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Identity_RolePermissions_Roles FOREIGN KEY (RoleId)
            REFERENCES [Identity].Roles (Id)
            ON DELETE CASCADE,
        CONSTRAINT UQ_Identity_RolePermissions_Unique UNIQUE (RoleId, Module, Entity, Action, Scope),
        CONSTRAINT CK_Identity_RolePermissions_Scope CHECK (Scope IS NULL OR (Scope >= 0 AND Scope <= 6))
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
    @value = N'Scope level (DataScopeLevel enum): 0=None, 1=Self, 2=EmployeeSet, 3=Position, 4=Department, 5=Company, 6=Global',
    @level0type = N'SCHEMA', @level0name = N'Identity',
    @level1type = N'TABLE', @level1name = N'RolePermissions',
    @level2type = N'COLUMN', @level2name = N'Scope'
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Optional company ID for company-scoped roles. NULL = tenant-global role, GUID = company-specific role.',
    @level0type = N'SCHEMA', @level0name = N'Identity',
    @level1type = N'TABLE', @level1name = N'Roles',
    @level2type = N'COLUMN', @level2name = N'CompanyId'
GO

PRINT 'Script 003_CreateRolesAndPermissionsTable.sql completed'
GO
