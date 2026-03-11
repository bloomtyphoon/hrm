-- =============================================
-- Script: Create EmployeeHierarchyClosures Table
-- Module: Personnel
-- Purpose: Materialized closure table for the employee management hierarchy.
--          Replaces the recursive CTE in GetAllSubordinateIdsAsync with O(1) lookup.
--          Maintained by EmployeeCreatedHierarchyClosureHandler and ManagerChangedDomainEventHandler.
-- Dependencies: 001_CreateEmployeesTable.sql
-- =============================================

-- Create EmployeeHierarchyClosures table
IF NOT EXISTS (
    SELECT 1 FROM sys.tables
    WHERE name = 'EmployeeHierarchyClosures' AND schema_id = SCHEMA_ID('Personnel')
)
BEGIN
    CREATE TABLE [Personnel].EmployeeHierarchyClosures
    (
        -- Composite primary key: one row per unique (ancestor, descendant) pair
        AncestorId   UNIQUEIDENTIFIER NOT NULL,
        DescendantId UNIQUEIDENTIFIER NOT NULL,

        -- Distance between ancestor and descendant (0 = self-reference)
        Depth        INT NOT NULL DEFAULT 0,

        -- Tenant ID for multi-tenant isolation (no FK to maintain module independence)
        TenantId     UNIQUEIDENTIFIER NOT NULL,

        CONSTRAINT PK_EmployeeHierarchyClosures PRIMARY KEY CLUSTERED (TenantId, AncestorId, DescendantId),
        CONSTRAINT CK_EmployeeHierarchyClosures_Depth CHECK (Depth >= 0)
    )

    PRINT 'Table [Personnel].[EmployeeHierarchyClosures] created successfully'
END
ELSE
BEGIN
    PRINT 'Table [Personnel].[EmployeeHierarchyClosures] already exists'
END
GO

-- =============================================
-- Indexes
-- =============================================

-- Primary lookup: "give me all descendants of manager X in tenant T"
-- Used by GetAllSubordinateIdsAsync and IsSubordinateOfAsync
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_EmployeeHierarchyClosures_AncestorId_TenantId'
      AND object_id = OBJECT_ID('[Personnel].EmployeeHierarchyClosures')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_EmployeeHierarchyClosures_AncestorId_TenantId
    ON [Personnel].EmployeeHierarchyClosures (AncestorId, TenantId)
    INCLUDE (DescendantId, Depth)

    PRINT 'Index IX_EmployeeHierarchyClosures_AncestorId_TenantId created'
END
GO

-- Reverse lookup: "give me all ancestors of employee Y in tenant T"
-- Used during closure table graft (ManagerChangedDomainEventHandler)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_EmployeeHierarchyClosures_DescendantId_TenantId'
      AND object_id = OBJECT_ID('[Personnel].EmployeeHierarchyClosures')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_EmployeeHierarchyClosures_DescendantId_TenantId
    ON [Personnel].EmployeeHierarchyClosures (DescendantId, TenantId)
    INCLUDE (AncestorId, Depth)

    PRINT 'Index IX_EmployeeHierarchyClosures_DescendantId_TenantId created'
END
GO

-- =============================================
-- Initial data population from existing hierarchy
--
-- For existing employees (those already in the database before this migration),
-- the closure table is populated using a recursive CTE that traverses the
-- ManagerId self-reference chain in Personnel.Employees.
--
-- New employees are handled by EmployeeCreatedHierarchyClosureHandler automatically.
-- =============================================

-- Populate closure table using a recursive CTE over the existing hierarchy.
-- This is a one-time backfill; ongoing maintenance is done by domain event handlers.
-- MAXRECURSION 0 is used to support arbitrarily deep hierarchies.
IF NOT EXISTS (SELECT TOP 1 1 FROM [Personnel].EmployeeHierarchyClosures)
BEGIN
    PRINT 'Populating EmployeeHierarchyClosures from existing hierarchy...'

    -- Use recursive CTE to enumerate all (ancestor, descendant, depth) pairs.
    ;WITH Hierarchy AS
    (
        -- Anchor: self-reference rows for every employee (Depth = 0)
        SELECT
            e.Id          AS AncestorId,
            e.Id          AS DescendantId,
            0             AS Depth,
            e.TenantId    AS TenantId
        FROM [Personnel].Employees e

        UNION ALL

        -- Recursive: walk up through each employee's manager chain
        SELECT
            h.AncestorId,
            e.Id          AS DescendantId,
            h.Depth + 1   AS Depth,
            e.TenantId    AS TenantId
        FROM [Personnel].Employees e
        INNER JOIN Hierarchy h ON h.DescendantId = e.ManagerId
        WHERE e.ManagerId IS NOT NULL
    )
    INSERT INTO [Personnel].EmployeeHierarchyClosures (AncestorId, DescendantId, Depth, TenantId)
    SELECT AncestorId, DescendantId, Depth, TenantId
    FROM Hierarchy
    OPTION (MAXRECURSION 0);

    PRINT 'EmployeeHierarchyClosures populated successfully'
END
ELSE
BEGIN
    PRINT 'EmployeeHierarchyClosures already has data — skipping backfill'
END
GO

-- Extended property
EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Closure table for employee management hierarchy. Enables O(1) subtree queries replacing recursive CTEs. Each row (AncestorId, DescendantId, Depth, TenantId) represents a reachable path in the hierarchy. Self-reference rows (AncestorId=DescendantId, Depth=0) act as existence markers.',
    @level0type = N'SCHEMA', @level0name = N'Personnel',
    @level1type = N'TABLE', @level1name = N'EmployeeHierarchyClosures'
GO

PRINT 'Script 005_CreateEmployeeHierarchyClosuresTable.sql completed'
GO
