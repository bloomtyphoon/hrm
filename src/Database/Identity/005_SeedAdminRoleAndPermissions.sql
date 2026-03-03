-- =============================================
-- Script: Seed Admin Role and Permissions
-- Module: Identity
-- Purpose: Create System Administrator role with full permissions across ALL modules
-- Dependencies: 001_CreateAccountsTable.sql, 003-004 tables
--
-- Scope values (DataScopeLevel enum):
--   0=None (deny), 1=Self, 2=EmployeeSet, 3=Position, 4=Department, 5=Company, 6=Global
--
-- Admin permissions use:
--   Scope=6 (Global) for management/view actions
--   Scope=1 (Self)   for self-service actions (CheckIn/CheckOut) which only allow Self scope
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
-- Step 2: Seed Permissions for ALL modules
-- =============================================
-- Scope=6 (Global) for all admin management permissions
-- Scope=1 (Self)   for employee self-service (CheckIn/CheckOut) — catalog only allows Self for these
-- =============================================

DECLARE @AdminRoleId UNIQUEIDENTIFIER
SELECT @AdminRoleId = Id FROM [Identity].Roles WHERE Name = 'System Administrator' AND IsDeleted = 0

IF @AdminRoleId IS NOT NULL
BEGIN
    -- Clear existing permissions (idempotent re-seeding)
    DELETE FROM [Identity].RolePermissions WHERE RoleId = @AdminRoleId

    -- =============================================
    -- Identity Module Permissions
    -- =============================================

    -- Account Entity
    INSERT INTO [Identity].RolePermissions (RoleId, Module, Entity, Action, Scope) VALUES
        (@AdminRoleId, 'Identity', 'Account', 'View',          6),  -- Global
        (@AdminRoleId, 'Identity', 'Account', 'Create',        6),
        (@AdminRoleId, 'Identity', 'Account', 'Update',        6),
        (@AdminRoleId, 'Identity', 'Account', 'Delete',        6),
        (@AdminRoleId, 'Identity', 'Account', 'ResetPassword', 6),
        (@AdminRoleId, 'Identity', 'Account', 'AssignRole',    6)

    -- Role Entity
    INSERT INTO [Identity].RolePermissions (RoleId, Module, Entity, Action, Scope) VALUES
        (@AdminRoleId, 'Identity', 'Role', 'View',             6),
        (@AdminRoleId, 'Identity', 'Role', 'Create',           6),
        (@AdminRoleId, 'Identity', 'Role', 'Update',           6),
        (@AdminRoleId, 'Identity', 'Role', 'Delete',           6),
        (@AdminRoleId, 'Identity', 'Role', 'AssignPermission', 6)

    -- SystemProfile Entity
    INSERT INTO [Identity].RolePermissions (RoleId, Module, Entity, Action, Scope) VALUES
        (@AdminRoleId, 'Identity', 'SystemProfile', 'View',             6),
        (@AdminRoleId, 'Identity', 'SystemProfile', 'Create',           6),
        (@AdminRoleId, 'Identity', 'SystemProfile', 'Update',           6),
        (@AdminRoleId, 'Identity', 'SystemProfile', 'ManageSuperAdmin', 6)

    -- EmployeeProfile Entity
    INSERT INTO [Identity].RolePermissions (RoleId, Module, Entity, Action, Scope) VALUES
        (@AdminRoleId, 'Identity', 'EmployeeProfile', 'View',   6),
        (@AdminRoleId, 'Identity', 'EmployeeProfile', 'Create', 6),
        (@AdminRoleId, 'Identity', 'EmployeeProfile', 'Update', 6)

    DECLARE @IdentityCount INT
    SELECT @IdentityCount = COUNT(*) FROM [Identity].RolePermissions
    WHERE RoleId = @AdminRoleId AND Module = 'Identity'
    PRINT 'Identity module permissions seeded: ' + CAST(@IdentityCount AS NVARCHAR(10))

    -- =============================================
    -- Organization Module Permissions
    -- =============================================

    -- Company Entity
    INSERT INTO [Identity].RolePermissions (RoleId, Module, Entity, Action, Scope) VALUES
        (@AdminRoleId, 'Organization', 'Company', 'View',   6),
        (@AdminRoleId, 'Organization', 'Company', 'Create', 6),
        (@AdminRoleId, 'Organization', 'Company', 'Update', 6)

    -- Department Entity
    INSERT INTO [Identity].RolePermissions (RoleId, Module, Entity, Action, Scope) VALUES
        (@AdminRoleId, 'Organization', 'Department', 'View',   6),
        (@AdminRoleId, 'Organization', 'Department', 'Create', 6),
        (@AdminRoleId, 'Organization', 'Department', 'Update', 6)

    -- Position Entity
    INSERT INTO [Identity].RolePermissions (RoleId, Module, Entity, Action, Scope) VALUES
        (@AdminRoleId, 'Organization', 'Position', 'View',   6),
        (@AdminRoleId, 'Organization', 'Position', 'Create', 6),
        (@AdminRoleId, 'Organization', 'Position', 'Update', 6)

    -- Tenant Entity (Global only)
    INSERT INTO [Identity].RolePermissions (RoleId, Module, Entity, Action, Scope) VALUES
        (@AdminRoleId, 'Organization', 'Tenant', 'View',   6),
        (@AdminRoleId, 'Organization', 'Tenant', 'Create', 6),
        (@AdminRoleId, 'Organization', 'Tenant', 'Update', 6)

    DECLARE @OrgCount INT
    SELECT @OrgCount = COUNT(*) FROM [Identity].RolePermissions
    WHERE RoleId = @AdminRoleId AND Module = 'Organization'
    PRINT 'Organization module permissions seeded: ' + CAST(@OrgCount AS NVARCHAR(10))

    -- =============================================
    -- Personnel Module Permissions
    -- =============================================

    -- Employee Entity
    INSERT INTO [Identity].RolePermissions (RoleId, Module, Entity, Action, Scope) VALUES
        (@AdminRoleId, 'Personnel', 'Employee', 'View',      6),
        (@AdminRoleId, 'Personnel', 'Employee', 'Create',    6),
        (@AdminRoleId, 'Personnel', 'Employee', 'Update',    6),
        (@AdminRoleId, 'Personnel', 'Employee', 'Terminate', 6)

    -- Assignment Entity
    INSERT INTO [Identity].RolePermissions (RoleId, Module, Entity, Action, Scope) VALUES
        (@AdminRoleId, 'Personnel', 'Assignment', 'View',   6),
        (@AdminRoleId, 'Personnel', 'Assignment', 'Create', 6),
        (@AdminRoleId, 'Personnel', 'Assignment', 'Update', 6)

    DECLARE @PersonnelCount INT
    SELECT @PersonnelCount = COUNT(*) FROM [Identity].RolePermissions
    WHERE RoleId = @AdminRoleId AND Module = 'Personnel'
    PRINT 'Personnel module permissions seeded: ' + CAST(@PersonnelCount AS NVARCHAR(10))

    -- =============================================
    -- Attendance Module Permissions
    -- =============================================
    -- Note: CheckIn/CheckOut are Self-only per PermissionCatalog (employee self-service).
    --       Admin still gets them with Scope=1 (Self) so they can check in as an employee.
    --       View and ManualRecord get Scope=6 (Global) for full oversight.

    INSERT INTO [Identity].RolePermissions (RoleId, Module, Entity, Action, Scope) VALUES
        (@AdminRoleId, 'Attendance', 'Record', 'CheckIn',      1),  -- Self only (catalog constraint)
        (@AdminRoleId, 'Attendance', 'Record', 'CheckOut',     1),  -- Self only (catalog constraint)
        (@AdminRoleId, 'Attendance', 'Record', 'View',         6),  -- Global
        (@AdminRoleId, 'Attendance', 'Record', 'ManualRecord', 6)   -- Global

    DECLARE @AttendanceCount INT
    SELECT @AttendanceCount = COUNT(*) FROM [Identity].RolePermissions
    WHERE RoleId = @AdminRoleId AND Module = 'Attendance'
    PRINT 'Attendance module permissions seeded: ' + CAST(@AttendanceCount AS NVARCHAR(10))

    -- Total summary
    DECLARE @TotalPermissions INT
    SELECT @TotalPermissions = COUNT(*) FROM [Identity].RolePermissions WHERE RoleId = @AdminRoleId
    PRINT 'Total permissions seeded for System Administrator: ' + CAST(@TotalPermissions AS NVARCHAR(10))
END
ELSE
BEGIN
    PRINT 'ERROR: System Administrator role not found. Run Step 1 first.'
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
    rp.Module,
    COUNT(rp.Id) AS PermissionCount
FROM [Identity].Roles r
INNER JOIN [Identity].RolePermissions rp ON r.Id = rp.RoleId
WHERE r.Name = 'System Administrator' AND r.IsDeleted = 0
GROUP BY r.Name, rp.Module
ORDER BY rp.Module

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
