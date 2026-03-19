using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
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

    public async Task<IReadOnlyCollection<Guid>> GetEmployeeCountryIdsAsync(
        Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .AsNoTracking()
            .Where(e => e.Id == employeeId && e.CountryId != null)
            .Select(e => e.CountryId!.Value)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Guid>> GetEmployeeRegionIdsAsync(
        Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .AsNoTracking()
            .Where(e => e.Id == employeeId && e.RegionId != null)
            .Select(e => e.RegionId!.Value)
            .ToListAsync(cancellationToken);
    }

    public async Task<ScopeDimensionIds> GetScopeDimensionIdsAsync(
        Guid employeeId, CancellationToken cancellationToken = default)
    {
        var activeAssignments = await _context.EmployeeAssignments
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId && a.Status == AssignmentStatus.Active)
            .ToListAsync(cancellationToken);

        var employee = await _context.Employees
            .AsNoTracking()
            .Where(e => e.Id == employeeId)
            .Select(e => new { e.CountryId, e.RegionId })
            .FirstOrDefaultAsync(cancellationToken);

        if (activeAssignments.Count == 0 && employee is null)
            return ScopeDimensionIds.Empty;

        return new ScopeDimensionIds.Builder()
            .Add(DimensionKeys.Company, activeAssignments.Select(a => a.CompanyId).Distinct().ToList())
            .Add(DimensionKeys.Department, activeAssignments.Select(a => a.DepartmentId).Distinct().ToList())
            .Add(DimensionKeys.Position, activeAssignments.Select(a => a.PositionId).Distinct().ToList())
            .Add(DimensionKeys.Country, employee?.CountryId is { } cid ? new[] { cid } : Array.Empty<Guid>())
            .Add(DimensionKeys.Region, employee?.RegionId is { } rid ? new[] { rid } : Array.Empty<Guid>())
            .Build();
    }
}
