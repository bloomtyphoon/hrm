-- =============================================
-- Script: Create Tenants Table
-- Module: Organization
-- Purpose: Add Tenants table and seed the System Tenant
-- Dependencies: 001_CreateCompaniesTable.sql (schema already exists)
-- =============================================

-- Create Tenants table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Tenants' AND schema_id = SCHEMA_ID('Organization'))
BEGIN
    CREATE TABLE [Organization].[Tenants]
    (
        -- Primary Key
        Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID(),

        -- Tenant Identity
        Code            NVARCHAR(50)        NOT NULL,
        Name            NVARCHAR(200)       NOT NULL,

        -- Status: 1=Active, 2=Suspended, 3=Deactivated
        Status          INT                 NOT NULL DEFAULT 1,

        -- System tenant flag: prevents modification/deletion of system tenant
        IsSystemTenant  BIT                 NOT NULL DEFAULT 0,

        -- Soft delete
        IsDeleted       BIT                 NOT NULL DEFAULT 0,
        DeletedAtUtc    DATETIME2           NULL,
        DeletedById     UNIQUEIDENTIFIER    NULL,

        -- Audit fields
        CreatedAtUtc    DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
        ModifiedAtUtc   DATETIME2           NULL,
        CreatedById     UNIQUEIDENTIFIER    NULL,
        ModifiedById    UNIQUEIDENTIFIER    NULL,

        CONSTRAINT PK_Tenants PRIMARY KEY (Id)
    )

    PRINT 'Table [Organization].[Tenants] created successfully'
END
ELSE
BEGIN
    PRINT 'Table [Organization].[Tenants] already exists'
END
GO

-- Indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tenants_Code' AND object_id = OBJECT_ID('[Organization].[Tenants]'))
BEGIN
    CREATE UNIQUE INDEX IX_Tenants_Code
        ON [Organization].[Tenants] (Code)
        WHERE IsDeleted = 0
    PRINT 'Index IX_Tenants_Code created'
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tenants_Status' AND object_id = OBJECT_ID('[Organization].[Tenants]'))
BEGIN
    CREATE INDEX IX_Tenants_Status ON [Organization].[Tenants] (Status)
    PRINT 'Index IX_Tenants_Status created'
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tenants_IsSystemTenant' AND object_id = OBJECT_ID('[Organization].[Tenants]'))
BEGIN
    CREATE INDEX IX_Tenants_IsSystemTenant ON [Organization].[Tenants] (IsSystemTenant)
    PRINT 'Index IX_Tenants_IsSystemTenant created'
END
GO

-- =============================================
-- Seed: System Tenant
-- Id = FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF (WellKnownTenants.SystemTenantId)
-- IsSystemTenant = 1 — domain guard prevents modification
-- =============================================
DECLARE @SystemTenantId UNIQUEIDENTIFIER = 'FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF'

IF NOT EXISTS (SELECT 1 FROM [Organization].[Tenants] WHERE Id = @SystemTenantId)
BEGIN
    INSERT INTO [Organization].[Tenants]
        (Id, Code, Name, Status, IsSystemTenant, IsDeleted, CreatedAtUtc)
    VALUES
        (@SystemTenantId, 'SYSTEM', 'System Tenant', 1, 1, 0, GETUTCDATE())

    PRINT 'System Tenant seeded successfully'
END
ELSE
BEGIN
    PRINT 'System Tenant already exists'
END
GO
