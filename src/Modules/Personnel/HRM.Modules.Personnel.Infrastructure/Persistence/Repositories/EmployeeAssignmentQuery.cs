using HRM.Modules.Personnel.Application.Abstractions;
using HRM.Modules.Personnel.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Personnel.Infrastructure.Persistence.Repositories;

internal sealed class EmployeeAssignmentQuery : IEmployeeAssignmentQuery
{
    private readonly PersonnelDbContext _context;

    public EmployeeAssignmentQuery(PersonnelDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<Guid>> GetEmployeeCompanyIdsAsync(
        Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.EmployeeAssignments
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId && a.Status == AssignmentStatus.Active)
            .Select(a => a.CompanyId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Guid>> GetEmployeeDepartmentIdsAsync(
        Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.EmployeeAssignments
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId && a.Status == AssignmentStatus.Active)
            .Select(a => a.DepartmentId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Guid>> GetEmployeePositionIdsAsync(
        Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.EmployeeAssignments
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId && a.Status == AssignmentStatus.Active)
            .Select(a => a.PositionId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<ScopeDimensionIds> GetScopeDimensionIdsAsync(
        Guid employeeId, CancellationToken cancellationToken = default)
    {
        var activeAssignments = await _context.EmployeeAssignments
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId && a.Status == AssignmentStatus.Active)
            .ToListAsync(cancellationToken);

        if (activeAssignments.Count == 0)
            return ScopeDimensionIds.Empty;

        return new ScopeDimensionIds
        {
            CompanyIds = activeAssignments.Select(a => a.CompanyId).Distinct().ToList(),
            DepartmentIds = activeAssignments.Select(a => a.DepartmentId).Distinct().ToList(),
            PositionIds = activeAssignments.Select(a => a.PositionId).Distinct().ToList()
        };
    }
}
