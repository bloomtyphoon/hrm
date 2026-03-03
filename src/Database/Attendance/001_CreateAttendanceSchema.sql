-- =============================================
-- Script: Create Attendance Schema
-- Module: Attendance
-- Purpose: Create Attendance schema for the Attendance module
-- Dependencies: None (first script to run for Attendance module)
-- =============================================

-- Create Attendance schema if not exists
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'Attendance')
BEGIN
    EXEC('CREATE SCHEMA [Attendance]')
    PRINT 'Schema [Attendance] created successfully'
END
ELSE
BEGIN
    PRINT 'Schema [Attendance] already exists'
END
GO

PRINT 'Script 001_CreateAttendanceSchema.sql completed'
GO
