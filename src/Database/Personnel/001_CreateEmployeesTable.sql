-- =============================================
-- Script: Create Employees Table
-- Module: Personnel
-- Purpose: Create Personnel schema and Employees table with indexes
-- Dependencies: None (first script to run for Personnel module)
-- Note: PrimaryCompanyId, PrimaryDepartmentId, PrimaryPositionId are
--       weak references to Organization module (no FK constraints)
-- =============================================

-- Create Personnel schema if not exists
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'Personnel')
BEGIN
    EXEC('CREATE SCHEMA [Personnel]')
    PRINT 'Schema [Personnel] created successfully'
END
ELSE
BEGIN
    PRINT 'Schema [Personnel] already exists'
END
GO

-- Create Employees table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Employees' AND schema_id = SCHEMA_ID('Personnel'))
BEGIN
    CREATE TABLE [Personnel].Employees
    (
        -- Primary Key
        Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),

        -- Employee Identity
        EmployeeCode NVARCHAR(50) NOT NULL,
        FirstName NVARCHAR(100) NOT NULL,
        LastName NVARCHAR(100) NOT NULL,
        Email NVARCHAR(255) NOT NULL,
        Phone NVARCHAR(20) NULL,
        DateOfBirth DATE NULL,

        -- Employment Information
        HireDate DATE NOT NULL,
        TerminationDate DATE NULL,

        -- Status: 1=Active, 2=OnLeave, 3=Terminated, 4=PendingOnboarding
        Status INT NOT NULL DEFAULT 4,

        -- Manager (self-referencing)
        ManagerId UNIQUEIDENTIFIER NULL,

        -- Primary Assignment (weak references to Organization module - no FK constraints)
        PrimaryCompanyId UNIQUEIDENTIFIER NULL,
        PrimaryDepartmentId UNIQUEIDENTIFIER NULL,
        PrimaryPositionId UNIQUEIDENTIFIER NULL,

        -- Audit Trail
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ModifiedAtUtc DATETIME2 NULL,
        CreatedById UNIQUEIDENTIFIER NULL,
        ModifiedById UNIQUEIDENTIFIER NULL,

        -- Constraints
        CONSTRAINT PK_Employees PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Employees_EmployeeCode UNIQUE (EmployeeCode),
        CONSTRAINT UQ_Employees_Email UNIQUE (Email),
        CONSTRAINT CK_Employees_Status CHECK (Status IN (1, 2, 3, 4)),
        CONSTRAINT FK_Employees_Manager FOREIGN KEY (ManagerId)
            REFERENCES [Personnel].Employees (Id) ON DELETE NO ACTION
    )

    PRINT 'Table [Personnel].[Employees] created successfully'
END
ELSE
BEGIN
    PRINT 'Table [Personnel].[Employees] already exists'
END
GO

-- =============================================
-- Indexes
-- =============================================

-- Index 1: EmployeeCode (Unique, for lookup queries)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Employees_EmployeeCode' AND object_id = OBJECT_ID('[Personnel].Employees'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_Employees_EmployeeCode
    ON [Personnel].Employees (EmployeeCode)
    INCLUDE (FirstName, LastName, Email, Status)

    PRINT 'Index IX_Employees_EmployeeCode created'
END
GO

-- Index 2: Email (Unique, for uniqueness checks and login)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Employees_Email' AND object_id = OBJECT_ID('[Personnel].Employees'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_Employees_Email
    ON [Personnel].Employees (Email)
    INCLUDE (Id, EmployeeCode, FirstName, LastName)

    PRINT 'Index IX_Employees_Email created'
END
GO

-- Index 3: ManagerId (for hierarchy queries - direct reports)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Employees_ManagerId' AND object_id = OBJECT_ID('[Personnel].Employees'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Employees_ManagerId
    ON [Personnel].Employees (ManagerId)
    INCLUDE (Id, EmployeeCode, FirstName, LastName, Status)

    PRINT 'Index IX_Employees_ManagerId created'
END
GO

-- Index 4: PrimaryCompanyId (for company-scoped queries)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Employees_PrimaryCompanyId' AND object_id = OBJECT_ID('[Personnel].Employees'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Employees_PrimaryCompanyId
    ON [Personnel].Employees (PrimaryCompanyId)
    INCLUDE (Id, EmployeeCode, FirstName, LastName, Status)

    PRINT 'Index IX_Employees_PrimaryCompanyId created'
END
GO

-- Index 5: Status (for filtering)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Employees_Status' AND object_id = OBJECT_ID('[Personnel].Employees'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Employees_Status
    ON [Personnel].Employees (Status, CreatedAtUtc DESC)
    INCLUDE (Id, EmployeeCode, FirstName, LastName, Email, HireDate)

    PRINT 'Index IX_Employees_Status created'
END
GO

-- Extended properties
EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Employees table - personnel records with self-referencing manager hierarchy and weak references to Organization module',
    @level0type = N'SCHEMA', @level0name = N'Personnel',
    @level1type = N'TABLE', @level1name = N'Employees'
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Weak reference to Organization.Companies - no FK constraint to maintain module independence',
    @level0type = N'SCHEMA', @level0name = N'Personnel',
    @level1type = N'TABLE', @level1name = N'Employees',
    @level2type = N'COLUMN', @level2name = N'PrimaryCompanyId'
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Weak reference to Organization.Departments - no FK constraint to maintain module independence',
    @level0type = N'SCHEMA', @level0name = N'Personnel',
    @level1type = N'TABLE', @level1name = N'Employees',
    @level2type = N'COLUMN', @level2name = N'PrimaryDepartmentId'
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Weak reference to Organization.Positions - no FK constraint to maintain module independence',
    @level0type = N'SCHEMA', @level0name = N'Personnel',
    @level1type = N'TABLE', @level1name = N'Employees',
    @level2type = N'COLUMN', @level2name = N'PrimaryPositionId'
GO

PRINT 'Script 001_CreateEmployeesTable.sql completed'
GO
