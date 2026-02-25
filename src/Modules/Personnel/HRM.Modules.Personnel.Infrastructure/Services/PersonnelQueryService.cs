using HRM.BuildingBlocks.Application.Abstractions.Personnel;
using HRM.Modules.Personnel.Application.Abstractions;

namespace HRM.Modules.Personnel.Infrastructure.Services;

/// <summary>
/// Implementation of IPersonnelQuery for cross-module consumption.
///
/// Exposes Personnel data (employee company assignments) to other modules
/// via the BuildingBlocks contract — no direct module-to-module dependency.
///
/// Consumed by: Organization module (for DataScopeService company filtering)
/// </summary>
internal sealed class PersonnelQueryService : IPersonnelQuery
{
    private readonly IEmployeeAssignmentQuery _assignmentQuery;

    public PersonnelQueryService(IEmployeeAssignmentQuery assignmentQuery)
    {
        _assignmentQuery = assignmentQuery;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> GetEmployeeCompanyIdsAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        var companyIds = await _assignmentQuery.GetEmployeeCompanyIdsAsync(employeeId, cancellationToken);
        return companyIds.ToList();
    }
}
