using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.Modules.Organization.Application.Abstractions;

namespace HRM.Modules.Organization.Infrastructure.Services;

/// <summary>
/// Implementation of IHierarchyScopeResolver for the Organization module.
///
/// Resolves manager-subordinate hierarchies by traversing the ManagerId relationships.
/// Uses caching for frequently accessed hierarchies.
///
/// Performance considerations:
/// - Uses recursive CTE for database traversal
/// - Results are cached with short TTL (hierarchy changes are infrequent)
/// - For large organizations, consider materialized path or closure table
/// </summary>
public sealed class HierarchyScopeResolver : IHierarchyScopeResolver
{
    private readonly IEmployeeRepository _employeeRepository;

    public HierarchyScopeResolver(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Guid>> GetSubordinateIdsAsync(
        Guid managerId,
        bool includeIndirect = true,
        CancellationToken cancellationToken = default)
    {
        if (includeIndirect)
        {
            // Get all subordinates recursively (includes manager)
            return await _employeeRepository.GetAllSubordinateIdsAsync(managerId, cancellationToken);
        }

        // Get direct reports only
        var directReports = await _employeeRepository.GetDirectReportsAsync(managerId, cancellationToken);

        // Include manager + direct reports
        var result = new List<Guid> { managerId };
        result.AddRange(directReports.Select(e => e.Id));

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Guid>> GetDirectSubordinateIdsAsync(
        Guid managerId,
        CancellationToken cancellationToken = default)
    {
        var directReports = await _employeeRepository.GetDirectReportsAsync(managerId, cancellationToken);
        return directReports.Select(e => e.Id).ToList();
    }

    /// <inheritdoc />
    public async Task<bool> IsSubordinateOfAsync(
        Guid employeeId,
        Guid managerId,
        CancellationToken cancellationToken = default)
    {
        // Same employee is not a subordinate of themselves
        if (employeeId == managerId)
            return false;

        return await _employeeRepository.IsSubordinateOfAsync(employeeId, managerId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> GetManagementChainAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        return await _employeeRepository.GetManagementChainAsync(employeeId, cancellationToken);
    }
}
