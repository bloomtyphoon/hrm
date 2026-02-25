-- =============================================
-- Script: Create Positions Table
-- Module: Organization
-- Purpose: Create Positions table with indexes
-- Dependencies: 001_CreateCompaniesTable.sql, 002_CreateDepartmentsTable.sql
-- =============================================

-- Create Positions table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Positions' AND schema_id = SCHEMA_ID('Organization'))
BEGIN
    CREATE TABLE [Organization].Positions
    (
        -- Primary Key
        Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),

        -- Position Information
        Code NVARCHAR(50) NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        Description NVARCHAR(MAX) NULL,

        -- Relationships
        CompanyId UNIQUEIDENTIFIER NOT NULL,
        DepartmentId UNIQUEIDENTIFIER NULL,

        -- Position Attributes
        PositionLevel INT NOT NULL DEFAULT 1,
        IsManagement BIT NOT NULL DEFAULT 0,
        MaxHeadcount INT NULL,

        -- Status: 1=Active, 2=Inactive, 3=Closed
        Status INT NOT NULL DEFAULT 1,

        -- Audit Trail
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ModifiedAtUtc DATETIME2 NULL,
        CreatedById UNIQUEIDENTIFIER NULL,
        ModifiedById UNIQUEIDENTIFIER NULL,

        -- Constraints
        CONSTRAINT PK_Positions PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Positions_DepartmentId_Code UNIQUE (DepartmentId, Code),
        CONSTRAINT CK_Positions_Status CHECK (Status IN (1, 2, 3)),
        CONSTRAINT CK_Positions_PositionLevel CHECK (PositionLevel >= 1),
        CONSTRAINT CK_Positions_MaxHeadcount CHECK (MaxHeadcount IS NULL OR MaxHeadcount >= 1),
        CONSTRAINT FK_Positions_Companies FOREIGN KEY (CompanyId)
            REFERENCES [Organization].Companies (Id) ON DELETE CASCADE,
        CONSTRAINT FK_Positions_Departments FOREIGN KEY (DepartmentId)
            REFERENCES [Organization].Departments (Id) ON DELETE NO ACTION
    )

    PRINT 'Table [Organization].[Positions] created successfully'
END
ELSE
BEGIN
    PRINT 'Table [Organization].[Positions] already exists'
END
GO

-- =============================================
-- Indexes
-- =============================================

-- Index 1: DepartmentId + Code (Unique, for department-scoped lookups)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Positions_DepartmentId_Code' AND object_id = OBJECT_ID('[Organization].Positions'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_Positions_DepartmentId_Code
    ON [Organization].Positions (DepartmentId, Code)
    INCLUDE (Title, Status, CompanyId)

    PRINT 'Index IX_Positions_DepartmentId_Code created'
END
GO

-- Index 2: DepartmentId (for filtering positions by department)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Positions_DepartmentId' AND object_id = OBJECT_ID('[Organization].Positions'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Positions_DepartmentId
    ON [Organization].Positions (DepartmentId)
    INCLUDE (Id, Code, Title, Status, CompanyId, PositionLevel, IsManagement)

    PRINT 'Index IX_Positions_DepartmentId created'
END
GO

-- Index 3: CompanyId (for filtering positions by company)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Positions_CompanyId' AND object_id = OBJECT_ID('[Organization].Positions'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Positions_CompanyId
    ON [Organization].Positions (CompanyId)
    INCLUDE (Id, Code, Title, Status, DepartmentId)

    PRINT 'Index IX_Positions_CompanyId created'
END
GO

-- Index 4: Status (for filtering)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Positions_Status' AND object_id = OBJECT_ID('[Organization].Positions'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Positions_Status
    ON [Organization].Positions (Status)
    INCLUDE (Id, Code, Title, CompanyId, DepartmentId)

    PRINT 'Index IX_Positions_Status created'
END
GO

-- Extended properties
EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Positions table - job positions within departments, with headcount management',
    @level0type = N'SCHEMA', @level0name = N'Organization',
    @level1type = N'TABLE', @level1name = N'Positions'
GO

PRINT 'Script 003_CreatePositionsTable.sql completed'
GO
