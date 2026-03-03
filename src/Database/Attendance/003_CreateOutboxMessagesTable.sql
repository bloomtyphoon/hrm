-- =============================================
-- Script: Create OutboxMessages Table
-- Module: Attendance
-- Purpose: Create OutboxMessages table for Transactional Outbox pattern
-- Dependencies: 001_CreateAttendanceSchema.sql (schema creation)
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'OutboxMessages' AND schema_id = SCHEMA_ID('Attendance'))
BEGIN
    CREATE TABLE [Attendance].OutboxMessages
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
        CONSTRAINT PK_Attendance_OutboxMessages PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_Attendance_OutboxMessages_AttemptCount CHECK (AttemptCount >= 0)
    )

    PRINT 'Table [Attendance].[OutboxMessages] created successfully'
END
ELSE
BEGIN
    PRINT 'Table [Attendance].[OutboxMessages] already exists'
END
GO

-- =============================================
-- Indexes
-- =============================================

-- Index 1: Unprocessed messages (for OutboxProcessor polling)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Attendance_OutboxMessages_ProcessedOnUtc' AND object_id = OBJECT_ID('[Attendance].OutboxMessages'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Attendance_OutboxMessages_ProcessedOnUtc
    ON [Attendance].OutboxMessages (ProcessedOnUtc)
    INCLUDE (Type, Content, OccurredOnUtc, AttemptCount)
    WHERE ProcessedOnUtc IS NULL

    PRINT 'Index IX_Attendance_OutboxMessages_ProcessedOnUtc created'
END
GO

-- Index 2: Chronological ordering
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Attendance_OutboxMessages_OccurredOnUtc' AND object_id = OBJECT_ID('[Attendance].OutboxMessages'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Attendance_OutboxMessages_OccurredOnUtc
    ON [Attendance].OutboxMessages (OccurredOnUtc)

    PRINT 'Index IX_Attendance_OutboxMessages_OccurredOnUtc created'
END
GO

-- Index 3: Retryable messages
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Attendance_OutboxMessages_Processing' AND object_id = OBJECT_ID('[Attendance].OutboxMessages'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Attendance_OutboxMessages_Processing
    ON [Attendance].OutboxMessages (ProcessedOnUtc, AttemptCount, OccurredOnUtc)
    INCLUDE (Type, Content, Error)
    WHERE ProcessedOnUtc IS NULL

    PRINT 'Index IX_Attendance_OutboxMessages_Processing created'
END
GO

-- Extended properties
EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'OutboxMessages table - Transactional Outbox pattern for Attendance module integration events',
    @level0type = N'SCHEMA', @level0name = N'Attendance',
    @level1type = N'TABLE', @level1name = N'OutboxMessages'
GO

PRINT 'Script 003_CreateOutboxMessagesTable.sql completed'
GO
