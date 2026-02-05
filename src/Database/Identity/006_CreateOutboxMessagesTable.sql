-- =============================================
-- Script: Create OutboxMessages Table
-- Module: Identity
-- Purpose: Create OutboxMessages table for Transactional Outbox pattern
-- Dependencies: 001_CreateAccountsTable.sql (schema creation)
-- =============================================

-- Create OutboxMessages table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'OutboxMessages' AND schema_id = SCHEMA_ID('Identity'))
BEGIN
    CREATE TABLE [Identity].OutboxMessages
    (
        -- Primary Key
        Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),

        -- Event Information
        Type NVARCHAR(500) NOT NULL,            -- Full type name of the integration event
        Content NVARCHAR(MAX) NOT NULL,         -- Serialized JSON content of the integration event
        OccurredOnUtc DATETIME2 NOT NULL,       -- When the domain event occurred (NOT when outbox message created)

        -- Processing State
        ProcessedOnUtc DATETIME2 NULL,          -- When the message was successfully processed (NULL = pending)
        Error NVARCHAR(2000) NULL,              -- Error message if processing failed
        AttemptCount INT NOT NULL DEFAULT 0,    -- Number of processing attempts

        -- Audit Trail
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ModifiedAtUtc DATETIME2 NULL,
        CreatedById UNIQUEIDENTIFIER NULL,
        ModifiedById UNIQUEIDENTIFIER NULL,

        -- Constraints
        CONSTRAINT PK_OutboxMessages PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_OutboxMessages_AttemptCount CHECK (AttemptCount >= 0)
    )

    PRINT 'Table [Identity].[OutboxMessages] created successfully'
END
ELSE
BEGIN
    PRINT 'Table [Identity].[OutboxMessages] already exists'
END
GO

-- =============================================
-- Indexes
-- =============================================

-- Index 1: Unprocessed messages (most critical for OutboxProcessor polling)
-- Optimized for: WHERE ProcessedOnUtc IS NULL ORDER BY OccurredOnUtc
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OutboxMessages_ProcessedOnUtc' AND object_id = OBJECT_ID('[Identity].OutboxMessages'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_OutboxMessages_ProcessedOnUtc
    ON [Identity].OutboxMessages (ProcessedOnUtc)
    INCLUDE (Type, Content, OccurredOnUtc, AttemptCount)
    WHERE ProcessedOnUtc IS NULL

    PRINT 'Index IX_OutboxMessages_ProcessedOnUtc created'
END
GO

-- Index 2: Chronological ordering (for processing order guarantee)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OutboxMessages_OccurredOnUtc' AND object_id = OBJECT_ID('[Identity].OutboxMessages'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_OutboxMessages_OccurredOnUtc
    ON [Identity].OutboxMessages (OccurredOnUtc)

    PRINT 'Index IX_OutboxMessages_OccurredOnUtc created'
END
GO

-- Index 3: Composite index for retryable messages query
-- Optimized for: WHERE ProcessedOnUtc IS NULL AND AttemptCount < @MaxAttempts ORDER BY OccurredOnUtc
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OutboxMessages_Processing' AND object_id = OBJECT_ID('[Identity].OutboxMessages'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_OutboxMessages_Processing
    ON [Identity].OutboxMessages (ProcessedOnUtc, AttemptCount, OccurredOnUtc)
    INCLUDE (Type, Content, Error)
    WHERE ProcessedOnUtc IS NULL

    PRINT 'Index IX_OutboxMessages_Processing created'
END
GO

-- Extended properties
EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'OutboxMessages table - Transactional Outbox pattern for reliable integration event publishing',
    @level0type = N'SCHEMA', @level0name = N'Identity',
    @level1type = N'TABLE', @level1name = N'OutboxMessages'
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Full qualified type name of the integration event (e.g. HRM.Modules.Identity.IntegrationEvents.AccountRegisteredIntegrationEvent)',
    @level0type = N'SCHEMA', @level0name = N'Identity',
    @level1type = N'TABLE', @level1name = N'OutboxMessages',
    @level2type = N'COLUMN', @level2name = N'Type'
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Serialized JSON content of the integration event. Deserialized by OutboxProcessor using Type for type resolution.',
    @level0type = N'SCHEMA', @level0name = N'Identity',
    @level1type = N'TABLE', @level1name = N'OutboxMessages',
    @level2type = N'COLUMN', @level2name = N'Content'
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'When the original domain event occurred. Used for chronological processing order.',
    @level0type = N'SCHEMA', @level0name = N'Identity',
    @level1type = N'TABLE', @level1name = N'OutboxMessages',
    @level2type = N'COLUMN', @level2name = N'OccurredOnUtc'
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'When the message was successfully processed and published. NULL means pending/unprocessed.',
    @level0type = N'SCHEMA', @level0name = N'Identity',
    @level1type = N'TABLE', @level1name = N'OutboxMessages',
    @level2type = N'COLUMN', @level2name = N'ProcessedOnUtc'
GO

PRINT 'Script 006_CreateOutboxMessagesTable.sql completed'
GO
