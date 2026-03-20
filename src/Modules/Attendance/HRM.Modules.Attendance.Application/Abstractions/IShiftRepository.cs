using HRM.Modules.Attendance.Domain.Entities;

namespace HRM.Modules.Attendance.Application.Abstractions;

public interface IShiftRepository
{
    Task<Shift?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, Guid tenantId, Guid? excludeId = null, CancellationToken cancellationToken = default);
    void Add(Shift shift);
    void Update(Shift shift);
    void Remove(Shift shift);
}
