using HRM.BuildingBlocks.Domain.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.BuildingBlocks.Domain.Entities;
using HRM.Modules.Attendance.Domain.Events;

namespace HRM.Modules.Attendance.Domain.Entities;

/// <summary>
/// Attendance record aggregate root - represents a single day's attendance for an employee.
///
/// DESIGN DECISIONS:
/// - Stores full UTC timestamps (DateTime) not TimeOnly to avoid midnight-crossing bugs
/// - DateOnly Date is derived from CheckInTimeUtc for daily uniqueness checks
/// - CompanyId is denormalized at check-in time for Company-scope filtering
/// - No FK to Personnel.Employee (module independence, same pattern as EmployeeAssignment)
/// - One record per employee per calendar day (enforced by DB unique index + domain logic)
/// - IsManualEntry distinguishes HR-created records from self check-in
///
/// Business rules enforced in domain:
/// - CheckOutTimeUtc must be after CheckInTimeUtc
/// - Cannot checkout if already checked out
/// </summary>
public class AttendanceRecord : AuditableEntity, IAggregateRoot, IScopedEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid EmployeeId { get; private set; }

    /// <summary>Calendar date derived from CheckInTimeUtc (UTC-based).</summary>
    public DateOnly Date { get; private set; }

    /// <summary>Full UTC timestamp of check-in.</summary>
    public DateTime CheckInTimeUtc { get; private set; }

    /// <summary>Full UTC timestamp of check-out. Null while still checked in.</summary>
    public DateTime? CheckOutTimeUtc { get; private set; }

    public AttendanceStatus Status { get; private set; }
    public string? Notes { get; private set; }

    /// <summary>True when this record was created by HR (not self check-in).</summary>
    public bool IsManualEntry { get; private set; }

    /// <summary>
    /// Employee's primary company at check-in time (denormalized).
    /// Used for Company-scope access filtering without cross-module join.
    /// </summary>
    [ScopeDimension(DimensionKeys.Company)]
    public Guid? CompanyId { get; private set; }

    /// <summary>IScopedEntity: employee owns their own attendance data.</summary>
    public Guid OwnerId => EmployeeId;

    private AttendanceRecord() { }

    /// <summary>
    /// Employee self check-in factory method.
    /// Raises <see cref="AttendanceCheckedInDomainEvent"/>.
    /// </summary>
    public static AttendanceRecord Create(
        Guid tenantId,
        Guid employeeId,
        Guid? companyId,
        DateTime checkInTimeUtc,
        string? notes = null)
    {
        var record = new AttendanceRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            Date = DateOnly.FromDateTime(checkInTimeUtc),
            CheckInTimeUtc = checkInTimeUtc,
            CompanyId = companyId,
            Notes = notes,
            Status = AttendanceStatus.CheckedIn,
            IsManualEntry = false
        };

        record.AddDomainEvent(new AttendanceCheckedInDomainEvent(
            tenantId, employeeId, record.Id, record.Date, checkInTimeUtc));

        return record;
    }

    /// <summary>
    /// HR manual record factory method (always complete with both check-in and check-out).
    /// Does not raise domain events (HR-recorded attendance, no real-time notification needed).
    /// </summary>
    public static AttendanceRecord CreateManual(
        Guid tenantId,
        Guid employeeId,
        Guid? companyId,
        DateOnly date,
        DateTime checkInTimeUtc,
        DateTime checkOutTimeUtc,
        string? notes = null)
    {
        if (checkOutTimeUtc <= checkInTimeUtc)
            throw new InvalidOperationException(
                "CheckOutTimeUtc must be after CheckInTimeUtc.");

        return new AttendanceRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            Date = date,
            CheckInTimeUtc = checkInTimeUtc,
            CheckOutTimeUtc = checkOutTimeUtc,
            CompanyId = companyId,
            Notes = notes,
            Status = AttendanceStatus.CheckedOut,
            IsManualEntry = true
        };
    }

    /// <summary>
    /// Employee checks out. Raises <see cref="AttendanceCheckedOutDomainEvent"/>.
    /// </summary>
    public void CheckOut(DateTime checkOutTimeUtc, string? notes = null)
    {
        if (Status == AttendanceStatus.CheckedOut)
            throw new InvalidOperationException("Already checked out.");

        if (checkOutTimeUtc <= CheckInTimeUtc)
            throw new InvalidOperationException(
                "CheckOutTimeUtc must be after CheckInTimeUtc.");

        CheckOutTimeUtc = checkOutTimeUtc;
        Status = AttendanceStatus.CheckedOut;

        if (notes is not null)
            Notes = notes;

        AddDomainEvent(new AttendanceCheckedOutDomainEvent(
            TenantId, EmployeeId, Id, Date, CheckInTimeUtc, checkOutTimeUtc));
    }
}
