using System.Reflection;
using HRM.BuildingBlocks.Application.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Infrastructure.Persistence;
using HRM.Modules.Attendance.Application.Abstractions.Data;
using HRM.Modules.Attendance.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Attendance.Infrastructure.Persistence;

/// <summary>
/// DbContext for Attendance module.
/// Inherits from ModuleDbContext for Unit of Work, domain events, soft delete, audit trail,
/// and automatic multi-tenant query filtering.
/// </summary>
public sealed class AttendanceDbContext : ModuleDbContext, IAttendanceQueryContext
{
    public AttendanceDbContext(
        DbContextOptions<AttendanceDbContext> options,
        IPublisher publisher,
        ITenantContext? tenantContext = null)
        : base(options, publisher, tenantContext)
    {
    }

    public override string ModuleName => "Attendance";

    public IQueryable<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("Attendance");

        modelBuilder.Entity<AttendanceRecord>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.Property(r => r.TenantId).IsRequired();
            entity.Property(r => r.EmployeeId).IsRequired();
            entity.Property(r => r.Date).IsRequired();
            entity.Property(r => r.CheckInTimeUtc).IsRequired();
            entity.Property(r => r.CheckOutTimeUtc);
            entity.Property(r => r.Status).IsRequired();
            entity.Property(r => r.IsManualEntry).IsRequired();
            entity.Property(r => r.Notes).HasMaxLength(500);
            entity.Property(r => r.CompanyId);

            // OwnerId is a computed property (=> EmployeeId), not stored
            entity.Ignore(r => r.OwnerId);

            // Unique index: one attendance record per employee per calendar day
            // Also serves as race-condition guard (DB-level enforcement)
            entity.HasIndex(r => new { r.EmployeeId, r.Date })
                .IsUnique()
                .HasDatabaseName("UX_AttendanceRecords_EmployeeDate");

            // Composite index for GetActiveCheckIn query: WHERE EmployeeId = X AND Date = Y AND Status = 1
            // Index order matches query selectivity: EmployeeId (high), Date (high), Status (low)
            entity.HasIndex(r => new { r.EmployeeId, r.Date, r.Status })
                .HasDatabaseName("IX_AttendanceRecords_ActiveLookup");

            // Index for tenant-scoped queries (applied by global query filter)
            entity.HasIndex(r => r.TenantId)
                .HasDatabaseName("IX_AttendanceRecords_TenantId");

            // Index for company-scoped queries (DataScopeLevel.Company)
            entity.HasIndex(r => r.CompanyId)
                .HasDatabaseName("IX_AttendanceRecords_CompanyId");
        });
    }
}
