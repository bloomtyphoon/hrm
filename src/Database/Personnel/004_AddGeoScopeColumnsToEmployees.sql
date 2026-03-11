-- =============================================
-- Script: Add Geographic Scope Columns to Employees
-- Module: Personnel
-- Purpose: Add CountryId and RegionId weak-reference columns for Country/Region scope support.
--          These columns are weak references to Organization module (no FK constraints).
--          Populated via SetGeographicScope() or integration events.
-- Dependencies: 001_CreateEmployeesTable.sql
-- =============================================

-- Add CountryId column (nullable; set when employee's country is known)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[Personnel].Employees') AND name = 'CountryId'
)
BEGIN
    ALTER TABLE [Personnel].Employees
    ADD CountryId UNIQUEIDENTIFIER NULL

    PRINT 'Column CountryId added to [Personnel].[Employees]'
END
ELSE
BEGIN
    PRINT 'Column CountryId already exists in [Personnel].[Employees]'
END
GO

-- Add RegionId column (nullable; set when employee's geographic region is known)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[Personnel].Employees') AND name = 'RegionId'
)
BEGIN
    ALTER TABLE [Personnel].Employees
    ADD RegionId UNIQUEIDENTIFIER NULL

    PRINT 'Column RegionId added to [Personnel].[Employees]'
END
ELSE
BEGIN
    PRINT 'Column RegionId already exists in [Personnel].[Employees]'
END
GO

-- =============================================
-- Indexes
-- =============================================

-- Index: CountryId (for Country scope queries)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Employees_CountryId' AND object_id = OBJECT_ID('[Personnel].Employees'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Employees_CountryId
    ON [Personnel].Employees (CountryId)
    WHERE CountryId IS NOT NULL

    PRINT 'Index IX_Employees_CountryId created'
END
GO

-- Index: RegionId (for Region scope queries)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Employees_RegionId' AND object_id = OBJECT_ID('[Personnel].Employees'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Employees_RegionId
    ON [Personnel].Employees (RegionId)
    WHERE RegionId IS NOT NULL

    PRINT 'Index IX_Employees_RegionId created'
END
GO

-- Extended properties
EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Weak reference to Organization.Countries - no FK constraint. Used for Country scope filtering via [ScopeDimension(Country)].',
    @level0type = N'SCHEMA', @level0name = N'Personnel',
    @level1type = N'TABLE', @level1name = N'Employees',
    @level2type = N'COLUMN', @level2name = N'CountryId'
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Weak reference to Organization.Regions - no FK constraint. Used for Region scope filtering via [ScopeDimension(Region)].',
    @level0type = N'SCHEMA', @level0name = N'Personnel',
    @level1type = N'TABLE', @level1name = N'Employees',
    @level2type = N'COLUMN', @level2name = N'RegionId'
GO

PRINT 'Script 004_AddGeoScopeColumnsToEmployees.sql completed'
GO
