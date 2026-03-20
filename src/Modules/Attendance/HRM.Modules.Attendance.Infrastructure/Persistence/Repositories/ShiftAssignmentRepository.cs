using HRM.Modules.Attendance.Application.Abstractions;
using HRM.Modules.Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Attendance.Infrastructure.Persistence.Repositories;

internal sealed class ShiftAssignmentRepository : IShiftAssignmentRepository
{
    private readonly AttendanceDbContext _context;

    public ShiftAssignmentRepository(AttendanceDbContext context) => _context = context;

    public async Task<ShiftAssignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Set<ShiftAssignment>()
            .FirstOrDefaultAsync(sa => sa.Id == id, cancellationToken);

    public void Add(ShiftAssignment assignment) => _context.Set<ShiftAssignment>().Add(assignment);
    public void Update(ShiftAssignment assignment) => _context.Set<ShiftAssignment>().Update(assignment);
    public void Remove(ShiftAssignment assignment) => _context.Set<ShiftAssignment>().Remove(assignment);
}
