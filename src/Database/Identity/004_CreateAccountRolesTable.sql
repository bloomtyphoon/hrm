-- =============================================
-- Script: Create AccountRoles Table
-- Module: Identity
-- Purpose: Many-to-many relationship between Accounts and Roles
-- Dependencies: 001_CreateAccountsTable.sql, 003_CreateRolesAndPermissionsTable.sql
-- =============================================

USE HrmDb
GO

-- =============================================
-- Create AccountRoles Junction Table
-- =============================================
-- Design: Many-to-many relationship
-- - One Account can have multiple Roles
-- - One Role can be assigned to multiple Accounts
-- - Includes assignment metadata (who assigned, when)
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AccountRoles' AND schema_id = SCHEMA_ID('Identity'))
BEGIN
    CREATE TABLE [Identity].AccountRoles
    (
        -- Composite Primary Key
        AccountId           UNIQUEIDENTIFIER    NOT NULL,
        RoleId              UNIQUEIDENTIFIER    NOT NULL,

        -- Assignment Metadata
        AssignedAtUtc       DATETIME2(7)        NOT NULL DEFAULT GETUTCDATE(),
        AssignedById        UNIQUEIDENTIFIER    NULL,

        -- Constraints
        CONSTRAINT PK_Identity_AccountRoles PRIMARY KEY CLUSTERED (AccountId, RoleId),
        CONSTRAINT FK_Identity_AccountRoles_Accounts FOREIGN KEY (AccountId)
            REFERENCES [Identity].Accounts (Id)
            ON DELETE CASCADE,
        CONSTRAINT FK_Identity_AccountRoles_Roles FOREIGN KEY (RoleId)
            REFERENCES [Identity].Roles (Id)
            ON DELETE CASCADE,
        CONSTRAINT FK_Identity_AccountRoles_AssignedBy FOREIGN KEY (AssignedById)
            REFERENCES [Identity].Accounts (Id)
            ON DELETE NO ACTION
    )

    PRINT 'Table [Identity].AccountRoles created successfully'
END
ELSE
BEGIN
    PRINT 'Table [Identity].AccountRoles already exists'
END
GO

-- =============================================
-- Indexes
-- =============================================

-- Index: Find all accounts with a role
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Identity_AccountRoles_RoleId' AND object_id = OBJECT_ID('[Identity].AccountRoles'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Identity_AccountRoles_RoleId
    ON [Identity].AccountRoles (RoleId)
    INCLUDE (AccountId, AssignedAtUtc)

    PRINT 'Index IX_Identity_AccountRoles_RoleId created'
END
GO

-- Index: Audit trail - who assigned roles
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Identity_AccountRoles_AssignedById' AND object_id = OBJECT_ID('[Identity].AccountRoles'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Identity_AccountRoles_AssignedById
    ON [Identity].AccountRoles (AssignedById)
    WHERE AssignedById IS NOT NULL

    PRINT 'Index IX_Identity_AccountRoles_AssignedById created'
END
GO

PRINT 'Script 004_CreateAccountRolesTable.sql completed'
GO
