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
    public IQueryable<Shift> Shifts => Set<Shift>();
    public IQueryable<ShiftAssignment> ShiftAssignments => Set<ShiftAssignment>();
    public IQueryable<LeaveType> LeaveTypes => Set<LeaveType>();
    public IQueryable<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public IQueryable<LeaveApprovalSetting> LeaveApprovalSettings => Set<LeaveApprovalSetting>();
    public IQueryable<LeaveApprovalStep> LeaveApprovalSteps => Set<LeaveApprovalStep>();

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

            entity.Ignore(r => r.OwnerId);

            entity.HasIndex(r => new { r.EmployeeId, r.Date })
                .IsUnique()
                .HasDatabaseName("UX_AttendanceRecords_EmployeeDate");

            entity.HasIndex(r => new { r.EmployeeId, r.Date, r.Status })
                .HasDatabaseName("IX_AttendanceRecords_ActiveLookup");

            entity.HasIndex(r => r.TenantId)
                .HasDatabaseName("IX_AttendanceRecords_TenantId");

            entity.HasIndex(r => r.CompanyId)
                .HasDatabaseName("IX_AttendanceRecords_CompanyId");
        });

        modelBuilder.Entity<Shift>(entity =>
        {
            entity.HasKey(s => s.Id);

            entity.Property(s => s.TenantId).IsRequired();
            entity.Property(s => s.Name).IsRequired().HasMaxLength(200);
            entity.Property(s => s.StartTime).IsRequired();
            entity.Property(s => s.EndTime).IsRequired();
            entity.Property(s => s.Description).HasMaxLength(500);
            entity.Property(s => s.IsActive).IsRequired();
            entity.Property(s => s.CompanyId);

            entity.Ignore(s => s.OwnerId);

            entity.HasIndex(s => new { s.TenantId, s.Name })
                .IsUnique()
                .HasDatabaseName("UX_Shifts_TenantName");

            entity.HasIndex(s => s.CompanyId)
                .HasDatabaseName("IX_Shifts_CompanyId");
        });

        modelBuilder.Entity<ShiftAssignment>(entity =>
        {
            entity.HasKey(sa => sa.Id);

            entity.Property(sa => sa.TenantId).IsRequired();
            entity.Property(sa => sa.ShiftId).IsRequired();
            entity.Property(sa => sa.EmployeeId).IsRequired();
            entity.Property(sa => sa.EffectiveFrom).IsRequired();
            entity.Property(sa => sa.EffectiveTo);
            entity.Property(sa => sa.CompanyId);

            entity.Ignore(sa => sa.OwnerId);

            entity.HasIndex(sa => new { sa.EmployeeId, sa.EffectiveFrom })
                .HasDatabaseName("IX_ShiftAssignments_EmployeeEffective");

            entity.HasIndex(sa => sa.ShiftId)
                .HasDatabaseName("IX_ShiftAssignments_ShiftId");

            entity.HasIndex(sa => sa.CompanyId)
                .HasDatabaseName("IX_ShiftAssignments_CompanyId");
        });

        modelBuilder.Entity<LeaveType>(entity =>
        {
            entity.HasKey(lt => lt.Id);

            entity.Property(lt => lt.TenantId).IsRequired();
            entity.Property(lt => lt.Name).IsRequired().HasMaxLength(200);
            entity.Property(lt => lt.Description).HasMaxLength(500);
            entity.Property(lt => lt.DefaultDaysPerYear).IsRequired();
            entity.Property(lt => lt.IsPaid).IsRequired();
            entity.Property(lt => lt.IsActive).IsRequired();

            entity.HasIndex(lt => new { lt.TenantId, lt.Name })
                .IsUnique()
                .HasDatabaseName("UX_LeaveTypes_TenantName");
        });

        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.HasKey(lr => lr.Id);

            entity.Property(lr => lr.TenantId).IsRequired();
            entity.Property(lr => lr.EmployeeId).IsRequired();
            entity.Property(lr => lr.LeaveTypeId).IsRequired();
            entity.Property(lr => lr.StartDate).IsRequired();
            entity.Property(lr => lr.EndDate).IsRequired();
            entity.Property(lr => lr.Reason).HasMaxLength(1000);
            entity.Property(lr => lr.Status).IsRequired();
            entity.Property(lr => lr.ApprovedByEmployeeId);
            entity.Property(lr => lr.DecisionDateUtc);
            entity.Property(lr => lr.DecisionNotes).HasMaxLength(1000);
            entity.Property(lr => lr.CompanyId);

            entity.Ignore(lr => lr.OwnerId);
            entity.Ignore(lr => lr.TotalDays);

            entity.HasIndex(lr => lr.EmployeeId)
                .HasDatabaseName("IX_LeaveRequests_EmployeeId");

            entity.HasIndex(lr => lr.LeaveTypeId)
                .HasDatabaseName("IX_LeaveRequests_LeaveTypeId");

            entity.HasIndex(lr => lr.Status)
                .HasDatabaseName("IX_LeaveRequests_Status");

            entity.HasIndex(lr => lr.CompanyId)
                .HasDatabaseName("IX_LeaveRequests_CompanyId");
        });

        modelBuilder.Entity<LeaveApprovalSetting>(entity =>
        {
            entity.HasKey(s => s.Id);

            entity.Property(s => s.TenantId).IsRequired();
            entity.Property(s => s.RequiresApproval).IsRequired();
            entity.Property(s => s.MaxApprovalLevels).IsRequired();
            entity.Property(s => s.AutoApproveIfDaysLessThanOrEqual);
            entity.Property(s => s.AllowSelfCancel).IsRequired();
            entity.Property(s => s.NotifyOnDecision).IsRequired();

            entity.HasIndex(s => s.TenantId)
                .IsUnique()
                .HasDatabaseName("UX_LeaveApprovalSettings_TenantId");
        });

        modelBuilder.Entity<LeaveApprovalStep>(entity =>
        {
            entity.HasKey(s => s.Id);

            entity.Property(s => s.TenantId).IsRequired();
            entity.Property(s => s.LeaveRequestId).IsRequired();
            entity.Property(s => s.StepOrder).IsRequired();
            entity.Property(s => s.ApproverEmployeeId).IsRequired();
            entity.Property(s => s.ApprovalLevelName).HasMaxLength(100);
            entity.Property(s => s.Status).IsRequired();
            entity.Property(s => s.DecisionDateUtc);
            entity.Property(s => s.Notes).HasMaxLength(1000);

            entity.HasIndex(s => new { s.LeaveRequestId, s.StepOrder })
                .IsUnique()
                .HasDatabaseName("UX_LeaveApprovalSteps_RequestStep");

            entity.HasIndex(s => s.ApproverEmployeeId)
                .HasDatabaseName("IX_LeaveApprovalSteps_ApproverId");
        });
    }
}
