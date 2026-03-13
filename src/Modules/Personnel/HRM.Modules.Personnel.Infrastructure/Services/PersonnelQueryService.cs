using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Personnel;
using HRM.Modules.Personnel.Application.Abstractions;

namespace HRM.Modules.Personnel.Infrastructure.Services;

/// <summary>
/// Implementation of cross-module query contracts for Personnel data.
///
/// Implements:
/// - IPersonnelQuery: company IDs for Organization module
/// - IEmployeeScopeDimensionProvider: all dimension IDs for shared DataScopeService
///
/// Both delegate to IEmployeeAssignmentQuery (Personnel-internal).
/// </summary>
internal sealed class PersonnelQueryService : IPersonnelQuery, IEmployeeScopeDimensionProvider
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

    /// <inheritdoc />
    public async Task<ScopeDimensionIds> GetScopeDimensionIdsAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        var dimensions = await _assignmentQuery.GetScopeDimensionIdsAsync(employeeId, cancellationToken);

        return new ScopeDimensionIds
        {
            CompanyIds = dimensions.CompanyIds,
            DepartmentIds = dimensions.DepartmentIds,
            PositionIds = dimensions.PositionIds,
            CountryIds = dimensions.CountryIds,
            RegionIds = dimensions.RegionIds
        };
    }
}
