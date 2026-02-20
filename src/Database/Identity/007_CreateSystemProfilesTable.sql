-- =============================================
-- Script: Create SystemProfiles Table
-- Module: Identity
-- Purpose: Extended profile data for System accounts (operators, admins)
-- Dependencies: 001_CreateAccountsTable.sql
-- =============================================

USE HrmDb
GO

-- =============================================
-- Create SystemProfiles Table
-- =============================================
-- Design: One-to-one relationship with Accounts
-- - Only for AccountType = 0 (System)
-- - Contains admin-specific data (department, job title, super admin flag)
-- - Separated from Account to keep auth entity lean
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SystemProfiles' AND schema_id = SCHEMA_ID('Identity'))
BEGIN
    CREATE TABLE [Identity].SystemProfiles
    (
        -- Primary Key
        Id                  UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID(),

        -- Account Reference (one-to-one)
        AccountId           UNIQUEIDENTIFIER    NOT NULL,

        -- Profile Data
        IsSuperAdmin        BIT                 NOT NULL DEFAULT 0,
        Department          NVARCHAR(200)       NULL,
        JobTitle            NVARCHAR(200)       NULL,
        Notes               NVARCHAR(2000)      NULL,

        -- Audit Trail
        CreatedAtUtc        DATETIME2(7)        NOT NULL DEFAULT GETUTCDATE(),
        ModifiedAtUtc       DATETIME2(7)        NULL,
        CreatedById         UNIQUEIDENTIFIER    NULL,
        ModifiedById        UNIQUEIDENTIFIER    NULL,

        -- Constraints
        CONSTRAINT PK_Identity_SystemProfiles PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Identity_SystemProfiles_Accounts FOREIGN KEY (AccountId)
            REFERENCES [Identity].Accounts (Id)
            ON DELETE CASCADE,
        CONSTRAINT UQ_Identity_SystemProfiles_AccountId UNIQUE (AccountId)
    )

    PRINT 'Table [Identity].SystemProfiles created successfully'
END
ELSE
BEGIN
    PRINT 'Table [Identity].SystemProfiles already exists'
END
GO

-- =============================================
-- Indexes
-- =============================================

-- Index: Lookup by AccountId (covered by unique constraint)
-- No additional index needed - UQ_Identity_SystemProfiles_AccountId serves as index

PRINT 'Script 007_CreateSystemProfilesTable.sql completed'
GO
