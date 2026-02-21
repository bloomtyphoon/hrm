using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Personnel;
using HRM.BuildingBlocks.Domain.Abstractions.Security;

namespace HRM.Modules.Organization.Infrastructure.Services;

/// <summary>
/// IDataScopeService implementation for the Organization module.
///
/// Resolves data scope rules for org structure queries (Company, Department, Position).
/// Uses employee company assignments from Personnel module (via IPersonnelQuery contract).
///
/// Architecture Flow:
///
/// User Request
///      │
///      ▼
/// IScopeGrantProvider (Identity) → scope LEVEL + employeeId
///      │
///      ▼
/// IPersonnelQuery (Personnel) → employee company IDs
///      │
///      ▼
/// DataScopeRule.Company([C1, C2, C3]) — multi-company ✓
///      │
///      ▼
/// Organization handler checks rule.DimensionIds.Contains(requestedCompanyId)
/// </summary>
internal sealed class OrganizationDataScopeService : IDataScopeService
{
    private readonly IScopeGrantProvider _grantProvider;
    private readonly IPersonnelQuery _personnelQuery;

    public OrganizationDataScopeService(
        IScopeGrantProvider grantProvider,
        IPersonnelQuery personnelQuery)
    {
        _grantProvider = grantProvider;
        _personnelQuery = personnelQuery;
    }

    /// <inheritdoc />
    public async Task<DataScopeRule> GetScopeRuleAsync(
        Guid userId,
        PermissionDescriptor permission,
        CancellationToken cancellationToken = default)
    {
        var grant = await _grantProvider.GetGrantAsync(userId, permission, cancellationToken);

        if (grant.IsSystemAccount || grant.Level == DataScopeLevel.Global)
            return DataScopeRule.Global();

        if (grant.Level == DataScopeLevel.None)
            return DataScopeRule.None();

        if (!grant.EmployeeId.HasValue)
            return DataScopeRule.None();

        // For Organization, company-level scope is the meaningful dimension.
        // Narrower scopes (Department, Position, Self, EmployeeSet) still restrict to employee's companies.
        var companyIds = await _personnelQuery.GetEmployeeCompanyIdsAsync(
            grant.EmployeeId.Value,
            cancellationToken);

        return companyIds.Count > 0
            ? DataScopeRule.Company(companyIds)
            : DataScopeRule.None();
    }
}
