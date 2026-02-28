-- =============================================
-- Script: Seed Admin Role and Permissions
-- Module: Identity
-- Purpose: Create System Administrator role with full permissions
-- Dependencies: 001_CreateAccountsTable.sql, 003-004 tables
-- =============================================

USE HrmDb
GO

-- =============================================
-- Step 1: Create System Administrator Role
-- =============================================
DECLARE @AdminRoleName NVARCHAR(100) = 'System Administrator'
DECLARE @AdminRoleDescription NVARCHAR(500) = 'Full system access. Reserved for system administrators only. Has all permissions across all modules.'
DECLARE @AdminRoleId UNIQUEIDENTIFIER

SELECT @AdminRoleId = Id FROM [Identity].Roles WHERE Name = @AdminRoleName AND IsDeleted = 0

IF @AdminRoleId IS NULL
BEGIN
    SET @AdminRoleId = NEWID()

    INSERT INTO [Identity].Roles
    (Id, TenantId, Name, Description, IsSystemRole, CreatedAtUtc, ModifiedAtUtc, CreatedById, ModifiedById, IsDeleted, DeletedAtUtc)
    VALUES
    (@AdminRoleId, 'FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF', @AdminRoleName, @AdminRoleDescription, 1, GETUTCDATE(), NULL, NULL, NULL, 0, NULL)

    PRINT 'System Administrator role created with ID: ' + CAST(@AdminRoleId AS NVARCHAR(50))
END
ELSE
BEGIN
    PRINT 'System Administrator role already exists with ID: ' + CAST(@AdminRoleId AS NVARCHAR(50))
END
GO

-- =============================================
-- Step 2: Seed Permissions
-- =============================================
-- Permission: Module.Entity.Action
-- Scope: 0=Global (system-wide access for admin)
-- =============================================

DECLARE @AdminRoleId UNIQUEIDENTIFIER
SELECT @AdminRoleId = Id FROM [Identity].Roles WHERE Name = 'System Administrator' AND IsDeleted = 0

IF @AdminRoleId IS NOT NULL
BEGIN
    -- Clear existing permissions (for re-seeding)
    DELETE FROM [Identity].RolePermissions WHERE RoleId = @AdminRoleId

    -- =============================================
    -- Identity Module Permissions
    -- =============================================

    -- Account Entity (unified: covers both System and Employee accounts)
    INSERT INTO [Identity].RolePermissions (RoleId, Module, Entity, Action, Scope) VALUES
        (@AdminRoleId, 'Identity', 'Account', 'View', 0),
        (@AdminRoleId, 'Identity', 'Account', 'Create', 0),
        (@AdminRoleId, 'Identity', 'Account', 'Update', 0),
        (@AdminRoleId, 'Identity', 'Account', 'Delete', 0),
        (@AdminRoleId, 'Identity', 'Account', 'ResetPassword', 0),
        (@AdminRoleId, 'Identity', 'Account', 'AssignRole', 0)

    -- Role Entity
    INSERT INTO [Identity].RolePermissions (RoleId, Module, Entity, Action, Scope) VALUES
        (@AdminRoleId, 'Identity', 'Role', 'View', 0),
        (@AdminRoleId, 'Identity', 'Role', 'Create', 0),
        (@AdminRoleId, 'Identity', 'Role', 'Update', 0),
        (@AdminRoleId, 'Identity', 'Role', 'Delete', 0),
        (@AdminRoleId, 'Identity', 'Role', 'AssignPermission', 0)

    -- Count total permissions
    DECLARE @TotalPermissions INT
    SELECT @TotalPermissions = COUNT(*) FROM [Identity].RolePermissions WHERE RoleId = @AdminRoleId
    PRINT 'Identity module permissions seeded: ' + CAST(@TotalPermissions AS NVARCHAR(10)) + ' permissions'
END
GO

-- =============================================
-- Step 3: Assign Admin Role to Admin Account
-- =============================================
DECLARE @AdminAccountId UNIQUEIDENTIFIER
DECLARE @AdminRoleId UNIQUEIDENTIFIER

SELECT @AdminAccountId = Id FROM [Identity].Accounts WHERE Username = 'admin' AND IsDeleted = 0
SELECT @AdminRoleId = Id FROM [Identity].Roles WHERE Name = 'System Administrator' AND IsDeleted = 0

IF @AdminAccountId IS NOT NULL AND @AdminRoleId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [Identity].AccountRoles WHERE AccountId = @AdminAccountId AND RoleId = @AdminRoleId)
    BEGIN
        INSERT INTO [Identity].AccountRoles (AccountId, RoleId, AssignedAtUtc, AssignedById)
        VALUES (@AdminAccountId, @AdminRoleId, GETUTCDATE(), NULL)

        PRINT 'System Administrator role assigned to admin account'
    END
    ELSE
    BEGIN
        PRINT 'Admin account already has System Administrator role'
    END
END
ELSE
BEGIN
    IF @AdminAccountId IS NULL
        PRINT 'WARNING: Admin account not found. Run 001_CreateAccountsTable.sql first.'
    IF @AdminRoleId IS NULL
        PRINT 'WARNING: System Administrator role not found.'
END
GO

-- =============================================
-- Verification
-- =============================================
PRINT ''
PRINT '================================='
PRINT 'Admin Role and Permissions Summary'
PRINT '================================='

SELECT
    r.Name AS RoleName,
    r.Description,
    r.IsSystemRole,
    COUNT(rp.Id) AS PermissionCount
FROM [Identity].Roles r
LEFT JOIN [Identity].RolePermissions rp ON r.Id = rp.RoleId
WHERE r.Name = 'System Administrator' AND r.IsDeleted = 0
GROUP BY r.Id, r.Name, r.Description, r.IsSystemRole

PRINT ''
PRINT 'Admin Account Role Assignment:'
SELECT
    a.Username,
    a.Email,
    r.Name AS RoleName,
    ar.AssignedAtUtc
FROM [Identity].Accounts a
INNER JOIN [Identity].AccountRoles ar ON a.Id = ar.AccountId
INNER JOIN [Identity].Roles r ON ar.RoleId = r.Id
WHERE a.Username = 'admin' AND a.IsDeleted = 0
GO

PRINT ''
PRINT 'Script 005_SeedAdminRoleAndPermissions.sql completed'
GO
