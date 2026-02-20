-- =============================================
-- Script: Create EmployeeAssignments Table
-- Module: Personnel
-- Purpose: Create EmployeeAssignments table for tracking employee assignments
--          to company/department/position combinations
-- Dependencies: 001_CreateEmployeesTable.sql
-- Note: CompanyId, DepartmentId, PositionId are weak references to
--       Organization module (no FK constraints)
-- =============================================

-- Create EmployeeAssignments table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EmployeeAssignments' AND schema_id = SCHEMA_ID('Personnel'))
BEGIN
    CREATE TABLE [Personnel].EmployeeAssignments
    (
        -- Primary Key
        Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),

        -- Employee Reference
        EmployeeId UNIQUEIDENTIFIER NOT NULL,

        -- Assignment Target (weak references to Organization module - no FK constraints)
        CompanyId UNIQUEIDENTIFIER NOT NULL,
        DepartmentId UNIQUEIDENTIFIER NOT NULL,
        PositionId UNIQUEIDENTIFIER NOT NULL,

        -- Assignment Period
        StartDate DATE NOT NULL,
        EndDate DATE NULL,

        -- Assignment Attributes
        IsPrimary BIT NOT NULL DEFAULT 0,

        -- Status: 1=Active, 2=Ended, 3=Pending, 4=Cancelled
        Status INT NOT NULL DEFAULT 3,

        -- Constraints
        CONSTRAINT PK_EmployeeAssignments PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_EmployeeAssignments_Status CHECK (Status IN (1, 2, 3, 4)),
        CONSTRAINT FK_EmployeeAssignments_Employees FOREIGN KEY (EmployeeId)
            REFERENCES [Personnel].Employees (Id) ON DELETE CASCADE
    )

    PRINT 'Table [Personnel].[EmployeeAssignments] created successfully'
END
ELSE
BEGIN
    PRINT 'Table [Personnel].[EmployeeAssignments] already exists'
END
GO

-- =============================================
-- Indexes
-- =============================================

-- Index 1: EmployeeId (for employee's assignments lookup)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EmployeeAssignments_EmployeeId' AND object_id = OBJECT_ID('[Personnel].EmployeeAssignments'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_EmployeeAssignments_EmployeeId
    ON [Personnel].EmployeeAssignments (EmployeeId)
    INCLUDE (CompanyId, DepartmentId, PositionId, Status, IsPrimary, StartDate, EndDate)

    PRINT 'Index IX_EmployeeAssignments_EmployeeId created'
END
GO

-- Index 2: CompanyId (for company-scoped assignment queries)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EmployeeAssignments_CompanyId' AND object_id = OBJECT_ID('[Personnel].EmployeeAssignments'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_EmployeeAssignments_CompanyId
    ON [Personnel].EmployeeAssignments (CompanyId)
    INCLUDE (EmployeeId, DepartmentId, PositionId, Status)

    PRINT 'Index IX_EmployeeAssignments_CompanyId created'
END
GO

-- Index 3: DepartmentId (for department-scoped assignment queries)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EmployeeAssignments_DepartmentId' AND object_id = OBJECT_ID('[Personnel].EmployeeAssignments'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_EmployeeAssignments_DepartmentId
    ON [Personnel].EmployeeAssignments (DepartmentId)
    INCLUDE (EmployeeId, CompanyId, PositionId, Status)

    PRINT 'Index IX_EmployeeAssignments_DepartmentId created'
END
GO

-- Index 4: PositionId (for position-scoped assignment queries)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EmployeeAssignments_PositionId' AND object_id = OBJECT_ID('[Personnel].EmployeeAssignments'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_EmployeeAssignments_PositionId
    ON [Personnel].EmployeeAssignments (PositionId)
    INCLUDE (EmployeeId, CompanyId, DepartmentId, Status)

    PRINT 'Index IX_EmployeeAssignments_PositionId created'
END
GO

-- Index 5: Composite (for duplicate assignment checks)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EmployeeAssignments_Composite' AND object_id = OBJECT_ID('[Personnel].EmployeeAssignments'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_EmployeeAssignments_Composite
    ON [Personnel].EmployeeAssignments (EmployeeId, CompanyId, DepartmentId, PositionId)
    INCLUDE (Status, IsPrimary, StartDate, EndDate)

    PRINT 'Index IX_EmployeeAssignments_Composite created'
END
GO

-- Extended properties
EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'EmployeeAssignments table - tracks employee assignments to company/department/position with period tracking',
    @level0type = N'SCHEMA', @level0name = N'Personnel',
    @level1type = N'TABLE', @level1name = N'EmployeeAssignments'
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Weak reference to Organization.Companies - no FK constraint to maintain module independence',
    @level0type = N'SCHEMA', @level0name = N'Personnel',
    @level1type = N'TABLE', @level1name = N'EmployeeAssignments',
    @level2type = N'COLUMN', @level2name = N'CompanyId'
GO

PRINT 'Script 002_CreateEmployeeAssignmentsTable.sql completed'
GO
