-- ============================================================
-- Migration: Create EmployeeHierarchyClosures table
-- Purpose:   Closure table for O(1) hierarchy queries
--
-- Pattern: Celko Closure Table
--   Each row represents a relationship between an ancestor and
--   a descendant at a given depth.
--   Depth=0 = self-reference (existence marker, enables "include self" semantics)
--   Depth=1 = direct report
--   Depth>1 = indirect subordinate
--
-- Maintained by:
--   EmployeeCreatedHierarchyHandler  - insert on employee creation
--   ManagerChangedDomainEventHandler - Celko prune+graft on manager change
--   RebuildHierarchyAsync            - full rebuild for bulk import / recovery
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'Personnel'
      AND TABLE_NAME   = 'EmployeeHierarchyClosures'
)
BEGIN
    CREATE TABLE Personnel.EmployeeHierarchyClosures
    (
        TenantId     UNIQUEIDENTIFIER NOT NULL,
        AncestorId   UNIQUEIDENTIFIER NOT NULL,
        DescendantId UNIQUEIDENTIFIER NOT NULL,
        Depth        INT              NOT NULL,

        CONSTRAINT PK_EmployeeHierarchyClosures
            PRIMARY KEY (TenantId, AncestorId, DescendantId)
    );

    -- Subtree lookup: "get all employees under manager X"
    -- Query: WHERE TenantId = @t AND AncestorId = @manager
    CREATE INDEX IX_EmployeeHierarchyClosures_AncestorId
        ON Personnel.EmployeeHierarchyClosures (TenantId, AncestorId, Depth);

    -- Management chain: "get all ancestors of employee X" (bottom-up)
    -- Query: WHERE TenantId = @t AND DescendantId = @emp ORDER BY Depth
    CREATE INDEX IX_EmployeeHierarchyClosures_DescendantId
        ON Personnel.EmployeeHierarchyClosures (TenantId, DescendantId, Depth);
END;
GO

-- ============================================================
-- Backfill existing employees into the closure table
-- Run once after creating the table on a live database.
-- ============================================================

-- Step 1: Self-reference rows for all active employees
INSERT INTO Personnel.EmployeeHierarchyClosures (TenantId, AncestorId, DescendantId, Depth)
SELECT TenantId, Id, Id, 0
FROM   Personnel.Employees
WHERE  IsDeleted = 0
  AND  NOT EXISTS (
      SELECT 1 FROM Personnel.EmployeeHierarchyClosures c
      WHERE c.TenantId = Personnel.Employees.TenantId
        AND c.AncestorId = Personnel.Employees.Id
        AND c.DescendantId = Personnel.Employees.Id
  );

-- Step 2: Build all ancestor-descendant paths using recursive CTE
;WITH Hierarchy AS (
    -- Base: direct parent-child relationships (Depth=1)
    SELECT
        e.TenantId,
        e.ManagerId   AS AncestorId,
        e.Id          AS DescendantId,
        1             AS Depth
    FROM Personnel.Employees e
    WHERE e.ManagerId IS NOT NULL
      AND e.IsDeleted  = 0

    UNION ALL

    -- Recursive: walk up the tree
    SELECT
        h.TenantId,
        e.ManagerId   AS AncestorId,
        h.DescendantId,
        h.Depth + 1
    FROM Hierarchy h
    INNER JOIN Personnel.Employees e
        ON e.Id       = h.AncestorId
        AND e.TenantId = h.TenantId
    WHERE e.ManagerId IS NOT NULL
)
INSERT INTO Personnel.EmployeeHierarchyClosures (TenantId, AncestorId, DescendantId, Depth)
SELECT DISTINCT h.TenantId, h.AncestorId, h.DescendantId, h.Depth
FROM   Hierarchy h
WHERE  NOT EXISTS (
    SELECT 1 FROM Personnel.EmployeeHierarchyClosures c
    WHERE c.TenantId     = h.TenantId
      AND c.AncestorId   = h.AncestorId
      AND c.DescendantId = h.DescendantId
);
GO
