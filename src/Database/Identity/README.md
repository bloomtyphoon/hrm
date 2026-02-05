# Identity Module Database Scripts

This directory contains SQL scripts for setting up the Identity module database schema and seed data.

## Overview

The Identity module uses SQL Server with schema separation (`Identity` schema) in a shared database (`HrmDb`).

**NO Entity Framework Migrations** - These scripts should be run manually or via deployment pipeline.

## Prerequisites

- SQL Server 2019 or later (SQL Server 2022 recommended)
- Database: `HrmDb` (created before running scripts)
- SQL Server Authentication or Windows Authentication
- User with `db_owner` or equivalent permissions

## Tables

| Table | Description |
|-------|-------------|
| `Identity.Accounts` | Unified authentication accounts (System + Employee) |
| `Identity.RefreshTokens` | JWT refresh tokens for session management |
| `Identity.Roles` | Role definitions for authorization |
| `Identity.RolePermissions` | Permissions assigned to roles (owned entity) |
| `Identity.AccountRoles` | Many-to-many: Accounts ↔ Roles |

## Script Execution Order

**CRITICAL**: Scripts must be executed in the following order:

1. **001_CreateAccountsTable.sql** - Schema + Accounts table + indexes + seed admin account
2. **002_CreateRefreshTokensTable.sql** - RefreshTokens table with polymorphic AccountType + AccountId
3. **003_CreateRolesAndPermissionsTable.sql** - Roles + RolePermissions tables
4. **004_CreateAccountRolesTable.sql** - AccountRoles junction table
5. **005_SeedAdminRoleAndPermissions.sql** - System Administrator role + permissions + assignment

## Quick Start

### Option 0: Automated Script (Recommended)

```bash
cd src/Database/Identity
./run-all-migrations.sh localhost HrmDb sa YourStrong@Passw0rd
```

### Option 1: sqlcmd (Command Line)

```bash
sqlcmd -S localhost -U sa -P YourPassword -d HrmDb -i 001_CreateAccountsTable.sql
sqlcmd -S localhost -U sa -P YourPassword -d HrmDb -i 002_CreateRefreshTokensTable.sql
sqlcmd -S localhost -U sa -P YourPassword -d HrmDb -i 003_CreateRolesAndPermissionsTable.sql
sqlcmd -S localhost -U sa -P YourPassword -d HrmDb -i 004_CreateAccountRolesTable.sql
sqlcmd -S localhost -U sa -P YourPassword -d HrmDb -i 005_SeedAdminRoleAndPermissions.sql
```

### Option 2: SSMS / Azure Data Studio

Open and execute each script in order (F5).

## Script Details

### 001_CreateAccountsTable.sql

**Purpose**: Creates Identity schema, Accounts table, indexes, and seeds admin account.

**Columns**:
- `Id` (UNIQUEIDENTIFIER, PK) - Account ID
- `Username` (NVARCHAR(50), UNIQUE) - Login username
- `Email` (NVARCHAR(255), UNIQUE) - Email address
- `PasswordHash` (NVARCHAR(255)) - BCrypt password hash
- `FullName` (NVARCHAR(200)) - Full name
- `PhoneNumber` (NVARCHAR(20)) - Phone (optional)
- `AccountType` (TINYINT) - 1=System, 2=Employee
- `Status` (INT) - 0=Pending, 1=Active, 2=Suspended, 3=Deactivated
- Security: `IsTwoFactorEnabled`, `FailedLoginAttempts`, `LockedUntilUtc`
- Audit: `CreatedAtUtc`, `ModifiedAtUtc`, `CreatedById`, `ModifiedById`
- Soft Delete: `IsDeleted`, `DeletedAtUtc`

**Indexes**: Username (unique), Email (unique), Status, CreatedAtUtc, AccountType, IsDeleted

**Seed**: Admin account (username: `admin`, password: `Admin@123456`, status: Active)

### 002_CreateRefreshTokensTable.sql

**Purpose**: Creates RefreshTokens table with polymorphic account reference.

**Key Columns**:
- `AccountType` (TINYINT) - 1=System, 2=Employee
- `AccountId` (UNIQUEIDENTIFIER) - References Accounts.Id
- `Token` (NVARCHAR(200), UNIQUE) - Refresh token value
- `ExpiresAt`, `RevokedAt`, `ReplacedByToken` - Lifecycle tracking
- `CreatedByIp`, `UserAgent` - Session tracking

**Indexes**: Active sessions (AccountType + AccountId + ExpiresAt), Token cleanup (ExpiresAt)

### 003_CreateRolesAndPermissionsTable.sql

**Purpose**: Creates Roles and RolePermissions tables.

**Roles**: Id, Name (unique), Description, IsSystemRole
**RolePermissions**: RoleId → Module.Entity.Action + Scope (0=Global, 1=Company, 2=Department, 3=Position, 4=Self)

### 004_CreateAccountRolesTable.sql

**Purpose**: Many-to-many junction between Accounts and Roles.

**Columns**: AccountId (PK, FK), RoleId (PK, FK), AssignedAtUtc, AssignedById

### 005_SeedAdminRoleAndPermissions.sql

**Purpose**: Seeds System Administrator role with all Identity permissions + assigns to admin account.

**Permissions seeded** (Scope = 0 = Global):
- `Identity.Account.*` (View, Create, Update, Delete, ResetPassword, AssignRole)
- `Identity.Role.*` (View, Create, Update, Delete, AssignPermission)

## Verification

```sql
-- Check tables
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'Identity'

-- Check admin account
SELECT Id, Username, Email, AccountType, Status FROM Identity.Accounts WHERE Username = 'admin'

-- Check admin role assignment
SELECT a.Username, r.Name AS RoleName, ar.AssignedAtUtc
FROM Identity.Accounts a
INNER JOIN Identity.AccountRoles ar ON a.Id = ar.AccountId
INNER JOIN Identity.Roles r ON ar.RoleId = r.Id
WHERE a.Username = 'admin'

-- Check permissions
SELECT Module, Entity, Action, Scope
FROM Identity.RolePermissions rp
INNER JOIN Identity.Roles r ON rp.RoleId = r.Id
WHERE r.Name = 'System Administrator'
```

## Rollback (Development Only)

```sql
DROP TABLE IF EXISTS [Identity].AccountRoles
DROP TABLE IF EXISTS [Identity].RolePermissions
DROP TABLE IF EXISTS [Identity].Roles
DROP TABLE IF EXISTS [Identity].RefreshTokens
DROP TABLE IF EXISTS [Identity].Accounts
DROP SCHEMA IF EXISTS [Identity]
```

## Connection String

```json
{
  "ConnectionStrings": {
    "HrmDatabase": "Server=localhost;Database=HrmDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;MultipleActiveResultSets=True"
  }
}
```
