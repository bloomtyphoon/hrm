using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Personnel;
using HRM.Modules.Personnel.Application.Abstractions;
using HRM.Modules.Personnel.Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Personnel.Infrastructure.Services;

/// <summary>
/// Implementation of cross-module query contracts for Personnel data.
///
/// Implements:
/// - IPersonnelQuery: company IDs, employee approval info for cross-module consumption
/// - IEmployeeScopeDimensionProvider: all dimension IDs for shared DataScopeService
/// </summary>
internal sealed class PersonnelQueryService : IPersonnelQuery, IEmployeeScopeDimensionProvider
{
    private readonly IEmployeeAssignmentQuery _assignmentQuery;
    private readonly IPersonnelQueryContext _queryContext;

    public PersonnelQueryService(
        IEmployeeAssignmentQuery assignmentQuery,
        IPersonnelQueryContext queryContext)
    {
        _assignmentQuery = assignmentQuery;
        _queryContext = queryContext;
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
    public async Task<EmployeeApprovalInfo?> GetEmployeeApprovalInfoAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        return await _queryContext.Employees
            .Where(e => e.Id == employeeId)
            .Select(e => new EmployeeApprovalInfo(
                e.Id,
                e.ManagerId,
                e.PrimaryDepartmentId,
                e.PrimaryCompanyId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ScopeDimensionIds> GetScopeDimensionIdsAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        return await _assignmentQuery.GetScopeDimensionIdsAsync(employeeId, cancellationToken);
    }
}
