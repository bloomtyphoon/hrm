-- =============================================
-- Script: Create OutboxMessages Table
-- Module: Organization
-- Purpose: Create OutboxMessages table for Transactional Outbox pattern
-- Dependencies: 001_CreateCompaniesTable.sql (schema creation)
-- =============================================

-- Create OutboxMessages table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'OutboxMessages' AND schema_id = SCHEMA_ID('Organization'))
BEGIN
    CREATE TABLE [Organization].OutboxMessages
    (
        -- Primary Key
        Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),

        -- Event Information
        Type NVARCHAR(500) NOT NULL,
        Content NVARCHAR(MAX) NOT NULL,
        OccurredOnUtc DATETIME2 NOT NULL,

        -- Processing State
        ProcessedOnUtc DATETIME2 NULL,
        Error NVARCHAR(2000) NULL,
        AttemptCount INT NOT NULL DEFAULT 0,

        -- Audit Trail
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ModifiedAtUtc DATETIME2 NULL,
        CreatedById UNIQUEIDENTIFIER NULL,
        ModifiedById UNIQUEIDENTIFIER NULL,

        -- Constraints
        CONSTRAINT PK_Organization_OutboxMessages PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_Organization_OutboxMessages_AttemptCount CHECK (AttemptCount >= 0)
    )

    PRINT 'Table [Organization].[OutboxMessages] created successfully'
END
ELSE
BEGIN
    PRINT 'Table [Organization].[OutboxMessages] already exists'
END
GO

-- =============================================
-- Indexes
-- =============================================

-- Index 1: Unprocessed messages (for OutboxProcessor polling)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Organization_OutboxMessages_ProcessedOnUtc' AND object_id = OBJECT_ID('[Organization].OutboxMessages'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Organization_OutboxMessages_ProcessedOnUtc
    ON [Organization].OutboxMessages (ProcessedOnUtc)
    INCLUDE (Type, Content, OccurredOnUtc, AttemptCount)
    WHERE ProcessedOnUtc IS NULL

    PRINT 'Index IX_Organization_OutboxMessages_ProcessedOnUtc created'
END
GO

-- Index 2: Chronological ordering
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Organization_OutboxMessages_OccurredOnUtc' AND object_id = OBJECT_ID('[Organization].OutboxMessages'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Organization_OutboxMessages_OccurredOnUtc
    ON [Organization].OutboxMessages (OccurredOnUtc)

    PRINT 'Index IX_Organization_OutboxMessages_OccurredOnUtc created'
END
GO

-- Index 3: Retryable messages
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Organization_OutboxMessages_Processing' AND object_id = OBJECT_ID('[Organization].OutboxMessages'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Organization_OutboxMessages_Processing
    ON [Organization].OutboxMessages (ProcessedOnUtc, AttemptCount, OccurredOnUtc)
    INCLUDE (Type, Content, Error)
    WHERE ProcessedOnUtc IS NULL

    PRINT 'Index IX_Organization_OutboxMessages_Processing created'
END
GO

-- Extended properties
EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'OutboxMessages table - Transactional Outbox pattern for Organization module integration events',
    @level0type = N'SCHEMA', @level0name = N'Organization',
    @level1type = N'TABLE', @level1name = N'OutboxMessages'
GO

PRINT 'Script 004_CreateOutboxMessagesTable.sql completed'
GO
