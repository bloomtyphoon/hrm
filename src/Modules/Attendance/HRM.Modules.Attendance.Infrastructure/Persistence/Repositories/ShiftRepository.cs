using HRM.Modules.Attendance.Application.Abstractions;
using HRM.Modules.Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Attendance.Infrastructure.Persistence.Repositories;

internal sealed class ShiftRepository : IShiftRepository
{
    private readonly AttendanceDbContext _context;

    public ShiftRepository(AttendanceDbContext context) => _context = context;

    public async Task<Shift?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Set<Shift>()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<bool> ExistsByNameAsync(string name, Guid tenantId, Guid? excludeId = null, CancellationToken cancellationToken = default)
        => await _context.Set<Shift>()
            .AnyAsync(s => s.Name == name && s.TenantId == tenantId && (!excludeId.HasValue || s.Id != excludeId.Value), cancellationToken);

    public void Add(Shift shift) => _context.Set<Shift>().Add(shift);
    public void Update(Shift shift) => _context.Set<Shift>().Update(shift);
    public void Remove(Shift shift) => _context.Set<Shift>().Remove(shift);
}
