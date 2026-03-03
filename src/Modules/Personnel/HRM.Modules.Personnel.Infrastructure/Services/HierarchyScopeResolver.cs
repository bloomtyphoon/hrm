using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.Modules.Personnel.Application.Abstractions;

namespace HRM.Modules.Personnel.Infrastructure.Services;

/// <summary>
/// Implementation of IHierarchyScopeResolver for the Personnel module.
///
/// Delegates to IEmployeeRepository which uses the closure table
/// (Personnel.EmployeeHierarchyClosures) for all hierarchy queries.
///
/// Self-inclusion semantics: all Resolve* methods include the manager themselves.
/// Rationale: managers always need to see their own data alongside their team's data.
/// </summary>
public sealed class HierarchyScopeResolver : IHierarchyScopeResolver
{
    private readonly IEmployeeRepository _employeeRepository;

    public HierarchyScopeResolver(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<Guid>> ResolveAllSubordinatesAsync(
        Guid managerId,
        CancellationToken cancellationToken = default)
    {
        return await _employeeRepository.GetAllSubordinateIdsAsync(managerId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<Guid>> ResolveDirectSubordinatesAsync(
        Guid managerId,
        CancellationToken cancellationToken = default)
    {
        var directReports = await _employeeRepository.GetDirectReportsAsync(managerId, cancellationToken);

        var result = new HashSet<Guid>(directReports.Select(e => e.Id)) { managerId };
        return result;
    }

    /// <inheritdoc />
    public async Task<bool> IsSubordinateOfAsync(
        Guid employeeId,
        Guid managerId,
        CancellationToken cancellationToken = default)
    {
        if (employeeId == managerId) return false;

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
