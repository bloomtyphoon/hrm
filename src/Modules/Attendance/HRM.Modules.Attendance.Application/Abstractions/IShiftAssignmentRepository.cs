using HRM.Modules.Attendance.Domain.Entities;

namespace HRM.Modules.Attendance.Application.Abstractions;

public interface IShiftAssignmentRepository
{
    Task<ShiftAssignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(ShiftAssignment assignment);
    void Update(ShiftAssignment assignment);
    void Remove(ShiftAssignment assignment);
}
