using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Domain.Abstractions.Security;

namespace HRM.BuildingBlocks.Infrastructure.Security;

/// <summary>
/// Shared implementation of IDataScopeService for all modules.
///
/// All modules resolve data scope from the same source: employee assignments.
/// This eliminates per-module duplication (Personnel, Identity, Organization
/// each had their own IDataScopeService implementation with identical logic).
///
/// Flow:
///   1. IScopeGrantProvider (Identity) → scope LEVEL + employeeId
///   2. IEmployeeScopeDimensionProvider (Personnel) → dimension IDs from assignments
///   3. IHierarchyScopeResolver (Personnel) → subordinate IDs for hierarchy scopes
///   4. Returns DataScopeRule ready for EF filtering
/// </summary>
public sealed class DataScopeService : IDataScopeService
{
    private readonly IScopeGrantProvider _grantProvider;
    private readonly IEmployeeScopeDimensionProvider _dimensionProvider;
    private readonly IHierarchyScopeResolver _hierarchyResolver;

    public DataScopeService(
        IScopeGrantProvider grantProvider,
        IEmployeeScopeDimensionProvider dimensionProvider,
        IHierarchyScopeResolver hierarchyResolver)
    {
        _grantProvider = grantProvider;
        _dimensionProvider = dimensionProvider;
        _hierarchyResolver = hierarchyResolver;
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

        var employeeId = grant.EmployeeId.Value;

        // Set-based scopes: resolve from hierarchy
        if (grant.Level == DataScopeLevel.Self)
            return DataScopeRule.Self(employeeId);

        if (grant.Level == DataScopeLevel.DirectReports)
        {
            var ids = await _hierarchyResolver.ResolveDirectSubordinatesAsync(employeeId, cancellationToken);
            return DataScopeRule.DirectReports(ids);
        }

        if (grant.Level == DataScopeLevel.EmployeeSet)
        {
            var ids = await _hierarchyResolver.ResolveAllSubordinatesAsync(employeeId, cancellationToken);
            return DataScopeRule.EmployeeSet(ids);
        }

        // Dimension-based scopes: resolve from employee assignments
        var dimensions = await _dimensionProvider.GetScopeDimensionIdsAsync(employeeId, cancellationToken);

        return grant.Level switch
        {
            DataScopeLevel.Position when dimensions.PositionIds.Count > 0 =>
                DataScopeRule.Position(dimensions.PositionIds),
            DataScopeLevel.Department when dimensions.DepartmentIds.Count > 0 =>
                DataScopeRule.Department(dimensions.DepartmentIds),
            DataScopeLevel.Company when dimensions.CompanyIds.Count > 0 =>
                DataScopeRule.Company(dimensions.CompanyIds),
            DataScopeLevel.Country when dimensions.CountryIds.Count > 0 =>
                DataScopeRule.Country(dimensions.CountryIds),
            DataScopeLevel.Region when dimensions.RegionIds.Count > 0 =>
                DataScopeRule.Region(dimensions.RegionIds),
            _ => DataScopeRule.None()
        };
    }
}
