-- =============================================
-- Script: Create Companies Table
-- Module: Organization
-- Purpose: Create Organization schema and Companies table with indexes
-- Dependencies: None (first script to run for Organization module)
-- =============================================

-- Create Organization schema if not exists
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'Organization')
BEGIN
    EXEC('CREATE SCHEMA [Organization]')
    PRINT 'Schema [Organization] created successfully'
END
ELSE
BEGIN
    PRINT 'Schema [Organization] already exists'
END
GO

-- Create Companies table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Companies' AND schema_id = SCHEMA_ID('Organization'))
BEGIN
    CREATE TABLE [Organization].Companies
    (
        -- Primary Key
        Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),

        -- Company Information
        Code NVARCHAR(50) NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        TaxId NVARCHAR(50) NULL,

        -- Status: 1=Active, 2=Inactive, 3=Pending
        Status INT NOT NULL DEFAULT 3,

        -- Audit Trail
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ModifiedAtUtc DATETIME2 NULL,
        CreatedById UNIQUEIDENTIFIER NULL,
        ModifiedById UNIQUEIDENTIFIER NULL,

        -- Constraints
        CONSTRAINT PK_Companies PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Companies_Code UNIQUE (Code),
        CONSTRAINT CK_Companies_Status CHECK (Status IN (1, 2, 3))
    )

    PRINT 'Table [Organization].[Companies] created successfully'
END
ELSE
BEGIN
    PRINT 'Table [Organization].[Companies] already exists'
END
GO

-- =============================================
-- Indexes
-- =============================================

-- Index 1: Code (Unique, for lookup queries)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Companies_Code' AND object_id = OBJECT_ID('[Organization].Companies'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_Companies_Code
    ON [Organization].Companies (Code)
    INCLUDE (Name, Status)

    PRINT 'Index IX_Companies_Code created'
END
GO

-- Index 2: Status (for filtering)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Companies_Status' AND object_id = OBJECT_ID('[Organization].Companies'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Companies_Status
    ON [Organization].Companies (Status, CreatedAtUtc DESC)
    INCLUDE (Id, Code, Name)

    PRINT 'Index IX_Companies_Status created'
END
GO

-- Extended properties
EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Companies table - organizational entities that own departments and positions',
    @level0type = N'SCHEMA', @level0name = N'Organization',
    @level1type = N'TABLE', @level1name = N'Companies'
GO

PRINT 'Script 001_CreateCompaniesTable.sql completed'
GO
