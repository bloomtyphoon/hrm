-- =============================================
-- Script: Create EmployeeProfiles Table
-- Module: Identity
-- Purpose: Extended profile data for Employee accounts
-- Dependencies: 001_CreateAccountsTable.sql
-- =============================================

USE HrmDb
GO

-- =============================================
-- Create EmployeeProfiles Table
-- =============================================
-- Design: One-to-one relationship with Accounts
-- - Only for AccountType = 1 (Employee)
-- - Bridges authentication (Account) with HR data (Employee in Personnel module)
-- - Contains data scope and organizational assignment info
-- - EmployeeId is an opaque reference to Personnel module
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EmployeeProfiles' AND schema_id = SCHEMA_ID('Identity'))
BEGIN
    CREATE TABLE [Identity].EmployeeProfiles
    (
        -- Primary Key
        Id                              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID(),

        -- Account Reference (one-to-one)
        AccountId                       UNIQUEIDENTIFIER    NOT NULL,

        -- Employee Reference (cross-module, opaque ID)
        EmployeeId                      UNIQUEIDENTIFIER    NOT NULL,

        -- Data Scope Configuration
        DefaultScopeLevel               INT                 NOT NULL DEFAULT 4, -- 0=Global, 1=Company, 2=Department, 3=Position, 4=Self
        CanAccessAllAssignedCompanies   BIT                 NOT NULL DEFAULT 1,

        -- Primary Organizational Assignments
        PrimaryCompanyId                UNIQUEIDENTIFIER    NULL,
        PrimaryDepartmentId             UNIQUEIDENTIFIER    NULL,
        PrimaryPositionId               UNIQUEIDENTIFIER    NULL,

        -- Audit Trail
        CreatedAtUtc                    DATETIME2(7)        NOT NULL DEFAULT GETUTCDATE(),
        ModifiedAtUtc                   DATETIME2(7)        NULL,
        CreatedById                     UNIQUEIDENTIFIER    NULL,
        ModifiedById                    UNIQUEIDENTIFIER    NULL,

        -- Constraints
        CONSTRAINT PK_Identity_EmployeeProfiles PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Identity_EmployeeProfiles_Accounts FOREIGN KEY (AccountId)
            REFERENCES [Identity].Accounts (Id)
            ON DELETE CASCADE,
        CONSTRAINT UQ_Identity_EmployeeProfiles_AccountId UNIQUE (AccountId),
        CONSTRAINT UQ_Identity_EmployeeProfiles_EmployeeId UNIQUE (EmployeeId),
        CONSTRAINT CK_Identity_EmployeeProfiles_ScopeLevel CHECK (DefaultScopeLevel IN (0, 1, 2, 3, 4))
    )

    PRINT 'Table [Identity].EmployeeProfiles created successfully'
END
ELSE
BEGIN
    PRINT 'Table [Identity].EmployeeProfiles already exists'
END
GO

-- =============================================
-- Indexes
-- =============================================

-- Indexes covered by unique constraints:
-- UQ_Identity_EmployeeProfiles_AccountId -> lookup by AccountId
-- UQ_Identity_EmployeeProfiles_EmployeeId -> lookup by EmployeeId

-- Index: Filter by company assignment
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Identity_EmployeeProfiles_PrimaryCompanyId' AND object_id = OBJECT_ID('[Identity].EmployeeProfiles'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Identity_EmployeeProfiles_PrimaryCompanyId
    ON [Identity].EmployeeProfiles (PrimaryCompanyId)
    WHERE PrimaryCompanyId IS NOT NULL

    PRINT 'Index IX_Identity_EmployeeProfiles_PrimaryCompanyId created'
END
GO

PRINT 'Script 008_CreateEmployeeProfilesTable.sql completed'
GO
