-- =============================================
-- Script: Add Subdomain column to Tenants table
-- Module: Organization
-- Purpose: Allow each tenant to be identified by a unique DNS subdomain label
--          e.g., "acme" maps to acme.hrm.example.com
-- =============================================

-- Add Subdomain column (nullable – existing tenants have no subdomain)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[Organization].[Tenants]')
      AND name = 'Subdomain'
)
BEGIN
    ALTER TABLE [Organization].[Tenants]
        ADD Subdomain NVARCHAR(63) NULL;

    PRINT 'Column [Organization].[Tenants].Subdomain added'
END
ELSE
BEGIN
    PRINT 'Column [Organization].[Tenants].Subdomain already exists'
END
GO

-- Unique filtered index: enforce globally unique non-null subdomains
-- Null values are excluded so tenants without a subdomain do not conflict
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Tenants_Subdomain'
      AND object_id = OBJECT_ID('[Organization].[Tenants]')
)
BEGIN
    CREATE UNIQUE INDEX IX_Tenants_Subdomain
        ON [Organization].[Tenants] (Subdomain)
        WHERE IsDeleted = 0 AND Subdomain IS NOT NULL;

    PRINT 'Index IX_Tenants_Subdomain created'
END
GO
