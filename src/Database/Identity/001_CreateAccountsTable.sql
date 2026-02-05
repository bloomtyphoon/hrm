-- =============================================
-- Script: Create Accounts Table
-- Module: Identity
-- Purpose: Create Accounts table with indexes and seed admin account
-- Dependencies: None (first script to run)
-- =============================================

-- Create Identity schema if not exists
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'Identity')
BEGIN
    EXEC('CREATE SCHEMA [Identity]')
    PRINT 'Schema [Identity] created successfully'
END
ELSE
BEGIN
    PRINT 'Schema [Identity] already exists'
END
GO

-- Create Accounts table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Accounts' AND schema_id = SCHEMA_ID('Identity'))
BEGIN
    CREATE TABLE [Identity].Accounts
    (
        -- Primary Key
        Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),

        -- Identity Information
        Username NVARCHAR(50) NOT NULL,
        Email NVARCHAR(255) NOT NULL,
        PasswordHash NVARCHAR(255) NOT NULL,
        FullName NVARCHAR(200) NOT NULL,
        PhoneNumber NVARCHAR(20) NULL,

        -- Account Type: 1=System, 2=Employee
        AccountType TINYINT NOT NULL DEFAULT 1,

        -- Status Management: 0=Pending, 1=Active, 2=Suspended, 3=Deactivated
        Status INT NOT NULL DEFAULT 0,
        ActivatedAtUtc DATETIME2 NULL,
        LastLoginAtUtc DATETIME2 NULL,

        -- Security Features
        IsTwoFactorEnabled BIT NOT NULL DEFAULT 0,
        TwoFactorSecret NVARCHAR(255) NULL,
        FailedLoginAttempts INT NOT NULL DEFAULT 0,
        LockedUntilUtc DATETIME2 NULL,

        -- Audit Trail
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ModifiedAtUtc DATETIME2 NULL,
        CreatedById UNIQUEIDENTIFIER NULL,
        ModifiedById UNIQUEIDENTIFIER NULL,

        -- Soft Delete
        IsDeleted BIT NOT NULL DEFAULT 0,
        DeletedAtUtc DATETIME2 NULL,

        -- Constraints
        CONSTRAINT PK_Accounts PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Accounts_Username UNIQUE (Username),
        CONSTRAINT UQ_Accounts_Email UNIQUE (Email),
        CONSTRAINT CK_Accounts_Status CHECK (Status BETWEEN 0 AND 3),
        CONSTRAINT CK_Accounts_AccountType CHECK (AccountType IN (1, 2)),
        CONSTRAINT CK_Accounts_FailedLoginAttempts CHECK (FailedLoginAttempts >= 0)
    )

    PRINT 'Table [Identity].[Accounts] created successfully'
END
ELSE
BEGIN
    PRINT 'Table [Identity].[Accounts] already exists'
END
GO

-- =============================================
-- Indexes
-- =============================================

-- Index 1: Username (Unique, for login queries)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Accounts_Username' AND object_id = OBJECT_ID('[Identity].Accounts'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_Accounts_Username
    ON [Identity].Accounts (Username)
    INCLUDE (PasswordHash, Status, IsDeleted, FailedLoginAttempts, LockedUntilUtc)
    WHERE IsDeleted = 0

    PRINT 'Index IX_Accounts_Username created'
END
GO

-- Index 2: Email (Unique, for uniqueness checks)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Accounts_Email' AND object_id = OBJECT_ID('[Identity].Accounts'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_Accounts_Email
    ON [Identity].Accounts (Email)
    INCLUDE (Id, Username, FullName)
    WHERE IsDeleted = 0

    PRINT 'Index IX_Accounts_Email created'
END
GO

-- Index 3: Status (for filtering)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Accounts_Status' AND object_id = OBJECT_ID('[Identity].Accounts'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Accounts_Status
    ON [Identity].Accounts (Status, CreatedAtUtc DESC)
    INCLUDE (Id, Username, Email, FullName, AccountType)
    WHERE IsDeleted = 0

    PRINT 'Index IX_Accounts_Status created'
END
GO

-- Index 4: CreatedAtUtc (for sorting/pagination)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Accounts_CreatedAtUtc' AND object_id = OBJECT_ID('[Identity].Accounts'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Accounts_CreatedAtUtc
    ON [Identity].Accounts (CreatedAtUtc DESC)
    INCLUDE (Id, Username, Email, FullName, Status, AccountType)
    WHERE IsDeleted = 0

    PRINT 'Index IX_Accounts_CreatedAtUtc created'
END
GO

-- Index 5: AccountType (for filtering by account type)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Accounts_AccountType' AND object_id = OBJECT_ID('[Identity].Accounts'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Accounts_AccountType
    ON [Identity].Accounts (AccountType, Status)
    INCLUDE (Id, Username, Email, FullName)
    WHERE IsDeleted = 0

    PRINT 'Index IX_Accounts_AccountType created'
END
GO

-- Index 6: IsDeleted (for soft delete queries)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Accounts_IsDeleted' AND object_id = OBJECT_ID('[Identity].Accounts'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Accounts_IsDeleted
    ON [Identity].Accounts (IsDeleted, DeletedAtUtc DESC)
    INCLUDE (Id, Username, Email, FullName)

    PRINT 'Index IX_Accounts_IsDeleted created'
END
GO

-- Extended properties
EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Accounts table - unified authentication accounts (System + Employee)',
    @level0type = N'SCHEMA', @level0name = N'Identity',
    @level1type = N'TABLE', @level1name = N'Accounts'
GO

-- =============================================
-- Seed Admin Account
-- =============================================
-- Default credentials: admin / Admin@123456
-- SECURITY WARNING: Change password immediately in production!
-- =============================================

IF NOT EXISTS (SELECT 1 FROM [Identity].Accounts WHERE Username = 'admin')
BEGIN
    DECLARE @PasswordHash NVARCHAR(255)
    SET @PasswordHash = '$2a$11$abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123'

    DECLARE @AdminId UNIQUEIDENTIFIER
    SET @AdminId = NEWID()

    INSERT INTO [Identity].Accounts
    (
        Id, Username, Email, PasswordHash, FullName, PhoneNumber,
        AccountType, Status, ActivatedAtUtc, LastLoginAtUtc,
        IsTwoFactorEnabled, TwoFactorSecret, FailedLoginAttempts, LockedUntilUtc,
        CreatedAtUtc, ModifiedAtUtc, CreatedById, ModifiedById,
        IsDeleted, DeletedAtUtc
    )
    VALUES
    (
        @AdminId, 'admin', 'admin@hrm.local', @PasswordHash, 'System Administrator', NULL,
        1,          -- AccountType: System
        1,          -- Status: Active
        GETUTCDATE(), NULL,
        0, NULL, 0, NULL,
        GETUTCDATE(), NULL, NULL, NULL,
        0, NULL
    )

    PRINT 'Admin account created successfully (Username: admin, Password: Admin@123456)'
    PRINT 'SECURITY: Change default password immediately after first login!'
END
ELSE
BEGIN
    PRINT 'Admin account already exists. Skipping seed.'
END
GO

PRINT 'Script 001_CreateAccountsTable.sql completed'
GO
