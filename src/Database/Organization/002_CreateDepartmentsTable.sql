-- =============================================
-- Script: Create Departments Table
-- Module: Organization
-- Purpose: Create Departments table with indexes and self-referencing hierarchy
-- Dependencies: 001_CreateCompaniesTable.sql
-- =============================================

-- Create Departments table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Departments' AND schema_id = SCHEMA_ID('Organization'))
BEGIN
    CREATE TABLE [Organization].Departments
    (
        -- Primary Key
        Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),

        -- Department Information
        Code NVARCHAR(50) NOT NULL,
        Name NVARCHAR(200) NOT NULL,

        -- Relationships
        CompanyId UNIQUEIDENTIFIER NOT NULL,
        ParentDepartmentId UNIQUEIDENTIFIER NULL,
        ManagerId UNIQUEIDENTIFIER NULL,

        -- Status: 1=Active, 2=Inactive, 3=Restructuring
        Status INT NOT NULL DEFAULT 1,

        -- Hierarchy
        Level INT NOT NULL DEFAULT 0,

        -- Audit Trail
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ModifiedAtUtc DATETIME2 NULL,
        CreatedById UNIQUEIDENTIFIER NULL,
        ModifiedById UNIQUEIDENTIFIER NULL,

        -- Constraints
        CONSTRAINT PK_Departments PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Departments_CompanyId_Code UNIQUE (CompanyId, Code),
        CONSTRAINT CK_Departments_Status CHECK (Status IN (1, 2, 3)),
        CONSTRAINT CK_Departments_Level CHECK (Level >= 0),
        CONSTRAINT FK_Departments_Companies FOREIGN KEY (CompanyId)
            REFERENCES [Organization].Companies (Id) ON DELETE CASCADE,
        CONSTRAINT FK_Departments_ParentDepartment FOREIGN KEY (ParentDepartmentId)
            REFERENCES [Organization].Departments (Id) ON DELETE NO ACTION
    )

    PRINT 'Table [Organization].[Departments] created successfully'
END
ELSE
BEGIN
    PRINT 'Table [Organization].[Departments] already exists'
END
GO

-- =============================================
-- Indexes
-- =============================================

-- Index 1: CompanyId + Code (Unique, for company-scoped lookups)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Departments_CompanyId_Code' AND object_id = OBJECT_ID('[Organization].Departments'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_Departments_CompanyId_Code
    ON [Organization].Departments (CompanyId, Code)
    INCLUDE (Name, Status, Level)

    PRINT 'Index IX_Departments_CompanyId_Code created'
END
GO

-- Index 2: CompanyId (for filtering departments by company)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Departments_CompanyId' AND object_id = OBJECT_ID('[Organization].Departments'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Departments_CompanyId
    ON [Organization].Departments (CompanyId)
    INCLUDE (Id, Code, Name, Status, ParentDepartmentId, Level)

    PRINT 'Index IX_Departments_CompanyId created'
END
GO

-- Index 3: ParentDepartmentId (for hierarchy queries)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Departments_ParentDepartmentId' AND object_id = OBJECT_ID('[Organization].Departments'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Departments_ParentDepartmentId
    ON [Organization].Departments (ParentDepartmentId)
    INCLUDE (Id, Code, Name, CompanyId)

    PRINT 'Index IX_Departments_ParentDepartmentId created'
END
GO

-- Extended properties
EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Departments table - hierarchical organizational units within companies',
    @level0type = N'SCHEMA', @level0name = N'Organization',
    @level1type = N'TABLE', @level1name = N'Departments'
GO

PRINT 'Script 002_CreateDepartmentsTable.sql completed'
GO
