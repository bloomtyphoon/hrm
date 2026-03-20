using HRM.BuildingBlocks.Application.Abstractions.Organization;
using HRM.Modules.Organization.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Organization.Infrastructure.Services;

/// <summary>
/// Implementation of cross-module query contract for Organization data.
/// Consumed by Personnel, Attendance, and other modules.
/// </summary>
internal sealed class OrganizationQueryService : IOrganizationQuery
{
    private readonly OrganizationDbContext _context;

    public OrganizationQueryService(OrganizationDbContext context) => _context = context;

    #region Company Queries

    public async Task<bool> CompanyExistsAsync(Guid companyId, CancellationToken cancellationToken = default)
        => await _context.Companies
            .AnyAsync(c => c.Id == companyId, cancellationToken);

    public async Task<OrganizationItemDto?> GetCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
        => await _context.Companies
            .Where(c => c.Id == companyId)
            .Select(c => new OrganizationItemDto
            {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name,
                IsActive = c.Status == Domain.Entities.CompanyStatus.Active
            })
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<OrganizationItemDto>> GetCompaniesAsync(
        IEnumerable<Guid> companyIds,
        CancellationToken cancellationToken = default)
    {
        var ids = companyIds.ToList();
        return await _context.Companies
            .Where(c => ids.Contains(c.Id))
            .Select(c => new OrganizationItemDto
            {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name,
                IsActive = c.Status == Domain.Entities.CompanyStatus.Active
            })
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Department Queries

    public async Task<Guid?> GetDepartmentManagerIdAsync(Guid departmentId, CancellationToken cancellationToken = default)
        => await _context.Departments
            .Where(d => d.Id == departmentId)
            .Select(d => d.ManagerId)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> DepartmentExistsAsync(Guid departmentId, CancellationToken cancellationToken = default)
        => await _context.Departments
            .AnyAsync(d => d.Id == departmentId, cancellationToken);

    public async Task<OrganizationItemDto?> GetDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default)
        => await _context.Departments
            .Where(d => d.Id == departmentId)
            .Select(d => new OrganizationItemDto
            {
                Id = d.Id,
                Code = d.Code,
                Name = d.Name,
                ParentId = d.CompanyId,
                IsActive = d.Status == Domain.Entities.DepartmentStatus.Active
            })
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<OrganizationItemDto>> GetDepartmentsAsync(
        IEnumerable<Guid> departmentIds,
        CancellationToken cancellationToken = default)
    {
        var ids = departmentIds.ToList();
        return await _context.Departments
            .Where(d => ids.Contains(d.Id))
            .Select(d => new OrganizationItemDto
            {
                Id = d.Id,
                Code = d.Code,
                Name = d.Name,
                ParentId = d.CompanyId,
                IsActive = d.Status == Domain.Entities.DepartmentStatus.Active
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OrganizationItemDto>> GetDepartmentsByCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
        => await _context.Departments
            .Where(d => d.CompanyId == companyId)
            .Select(d => new OrganizationItemDto
            {
                Id = d.Id,
                Code = d.Code,
                Name = d.Name,
                ParentId = d.CompanyId,
                IsActive = d.Status == Domain.Entities.DepartmentStatus.Active
            })
            .ToListAsync(cancellationToken);

    #endregion

    #region Position Queries

    public async Task<bool> PositionExistsAsync(Guid positionId, CancellationToken cancellationToken = default)
        => await _context.Positions
            .AnyAsync(p => p.Id == positionId, cancellationToken);

    public async Task<OrganizationItemDto?> GetPositionAsync(Guid positionId, CancellationToken cancellationToken = default)
        => await _context.Positions
            .Where(p => p.Id == positionId)
            .Select(p => new OrganizationItemDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                ParentId = p.DepartmentId,
                IsActive = p.Status == Domain.Entities.PositionStatus.Active
            })
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<OrganizationItemDto>> GetPositionsAsync(
        IEnumerable<Guid> positionIds,
        CancellationToken cancellationToken = default)
    {
        var ids = positionIds.ToList();
        return await _context.Positions
            .Where(p => ids.Contains(p.Id))
            .Select(p => new OrganizationItemDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                ParentId = p.DepartmentId,
                IsActive = p.Status == Domain.Entities.PositionStatus.Active
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OrganizationItemDto>> GetPositionsByDepartmentAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default)
        => await _context.Positions
            .Where(p => p.DepartmentId == departmentId)
            .Select(p => new OrganizationItemDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                ParentId = p.DepartmentId,
                IsActive = p.Status == Domain.Entities.PositionStatus.Active
            })
            .ToListAsync(cancellationToken);

    #endregion

    #region Validation

    public async Task<OrganizationValidationResult> ValidateAssignmentAsync(
        Guid companyId,
        Guid departmentId,
        Guid positionId,
        CancellationToken cancellationToken = default)
    {
        var company = await GetCompanyAsync(companyId, cancellationToken);
        if (company is null)
            return OrganizationValidationResult.Failure($"Company '{companyId}' not found.");

        var department = await GetDepartmentAsync(departmentId, cancellationToken);
        if (department is null)
            return OrganizationValidationResult.Failure($"Department '{departmentId}' not found.");

        if (department.ParentId != companyId)
            return OrganizationValidationResult.Failure(
                $"Department '{departmentId}' does not belong to company '{companyId}'.");

        var position = await GetPositionAsync(positionId, cancellationToken);
        if (position is null)
            return OrganizationValidationResult.Failure($"Position '{positionId}' not found.");

        if (position.ParentId != departmentId)
            return OrganizationValidationResult.Failure(
                $"Position '{positionId}' does not belong to department '{departmentId}'.");

        return OrganizationValidationResult.Success(company, department, position);
    }

    #endregion
}
