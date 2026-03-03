-- =============================================
-- Script: Create AttendanceRecords Table
-- Module: Attendance
-- Purpose: Create AttendanceRecords table with indexes
-- Dependencies: 001_CreateAttendanceSchema.sql (schema creation)
-- Note:
--   - CheckInTimeUtc / CheckOutTimeUtc use DATETIME2 (full UTC timestamps)
--   - Date is DATE (UTC-based date of check-in, Vietnam assumption: UTC+7)
--   - Status: 1=CheckedIn, 2=CheckedOut (no ManualEntry in status enum)
--   - IsManualEntry: separate BIT flag for HR-recorded entries
--   - CompanyId: denormalized from Personnel for scope-based filtering (no FK)
--   - No FK to Personnel.Employees for module independence
--   - Unique index on (EmployeeId, Date) enforces one record per employee per day
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AttendanceRecords' AND schema_id = SCHEMA_ID('Attendance'))
BEGIN
    CREATE TABLE [Attendance].AttendanceRecords
    (
        -- Primary Key
        Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),

        -- Multi-tenancy (soft reference, no FK to Organization.Tenants)
        TenantId UNIQUEIDENTIFIER NOT NULL,

        -- Employee (weak reference to Personnel.Employees — no FK for module independence)
        EmployeeId UNIQUEIDENTIFIER NOT NULL,

        -- Attendance Date (UTC-based; for Vietnam UTC+7, midnight UTC = 07:00 local)
        Date DATE NOT NULL,

        -- Check-in / Check-out timestamps (full UTC DATETIME2)
        CheckInTimeUtc DATETIME2 NOT NULL,
        CheckOutTimeUtc DATETIME2 NULL,

        -- Status: 1=CheckedIn, 2=CheckedOut
        Status INT NOT NULL DEFAULT 1,

        -- Manual entry flag (set by HR when recording retroactively)
        IsManualEntry BIT NOT NULL DEFAULT 0,

        -- Company scope (denormalized from Personnel at check-in time; weak reference)
        CompanyId UNIQUEIDENTIFIER NULL,

        -- Optional notes
        Notes NVARCHAR(500) NULL,

        -- Audit Trail
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ModifiedAtUtc DATETIME2 NULL,
        CreatedById UNIQUEIDENTIFIER NULL,
        ModifiedById UNIQUEIDENTIFIER NULL,

        -- Constraints
        CONSTRAINT PK_AttendanceRecords PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_AttendanceRecords_Status CHECK (Status IN (1, 2)),
        CONSTRAINT CK_AttendanceRecords_CheckOutAfterCheckIn
            CHECK (CheckOutTimeUtc IS NULL OR CheckOutTimeUtc > CheckInTimeUtc)
    )

    PRINT 'Table [Attendance].[AttendanceRecords] created successfully'
END
ELSE
BEGIN
    PRINT 'Table [Attendance].[AttendanceRecords] already exists'
END
GO

-- =============================================
-- Indexes
-- =============================================

-- Unique index: One attendance record per employee per day
-- Also prevents race-condition duplicates at DB level
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_AttendanceRecords_EmployeeDate' AND object_id = OBJECT_ID('[Attendance].AttendanceRecords'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_AttendanceRecords_EmployeeDate
    ON [Attendance].AttendanceRecords (EmployeeId, Date)

    PRINT 'Index UX_AttendanceRecords_EmployeeDate created'
END
GO

-- Composite index: Fast lookup for active check-in (check-out flow)
-- Order: EmployeeId → Date → Status (date before status for range queries)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AttendanceRecords_ActiveLookup' AND object_id = OBJECT_ID('[Attendance].AttendanceRecords'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AttendanceRecords_ActiveLookup
    ON [Attendance].AttendanceRecords (EmployeeId, Date, Status)
    INCLUDE (CheckInTimeUtc, CheckOutTimeUtc, Notes)

    PRINT 'Index IX_AttendanceRecords_ActiveLookup created'
END
GO

-- Index: TenantId (for tenant-scoped queries via global query filter)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AttendanceRecords_TenantId' AND object_id = OBJECT_ID('[Attendance].AttendanceRecords'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AttendanceRecords_TenantId
    ON [Attendance].AttendanceRecords (TenantId)

    PRINT 'Index IX_AttendanceRecords_TenantId created'
END
GO

-- Index: CompanyId (for company-scope filtering)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AttendanceRecords_CompanyId' AND object_id = OBJECT_ID('[Attendance].AttendanceRecords'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AttendanceRecords_CompanyId
    ON [Attendance].AttendanceRecords (CompanyId, Date DESC)
    INCLUDE (EmployeeId, Status, CheckInTimeUtc, CheckOutTimeUtc)
    WHERE CompanyId IS NOT NULL

    PRINT 'Index IX_AttendanceRecords_CompanyId created'
END
GO

-- Extended properties
EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'AttendanceRecords table - daily check-in/check-out records per employee. One record per employee per day enforced by unique index.',
    @level0type = N'SCHEMA', @level0name = N'Attendance',
    @level1type = N'TABLE', @level1name = N'AttendanceRecords'
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Weak reference to Personnel.Employees — no FK constraint to maintain module independence',
    @level0type = N'SCHEMA', @level0name = N'Attendance',
    @level1type = N'TABLE', @level1name = N'AttendanceRecords',
    @level2type = N'COLUMN', @level2name = N'EmployeeId'
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Denormalized from Personnel at check-in time; weak reference to Organization.Companies — no FK constraint',
    @level0type = N'SCHEMA', @level0name = N'Attendance',
    @level1type = N'TABLE', @level1name = N'AttendanceRecords',
    @level2type = N'COLUMN', @level2name = N'CompanyId'
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'UTC-based date. For Vietnam (UTC+7): 2024-01-15 check-in at 08:00 local = 2024-01-15 01:00 UTC → Date = 2024-01-15',
    @level0type = N'SCHEMA', @level0name = N'Attendance',
    @level1type = N'TABLE', @level1name = N'AttendanceRecords',
    @level2type = N'COLUMN', @level2name = N'Date'
GO

PRINT 'Script 002_CreateAttendanceRecordsTable.sql completed'
GO
