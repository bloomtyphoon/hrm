using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Domain.Abstractions.Security;

namespace HRM.Modules.Personnel.Infrastructure.Services;

/// <summary>
/// Extended data scope service supporting DataScopePolicy for multi-rule scenarios.
/// Uses the same shared dimension provider as the BuildingBlocks DataScopeService.
/// </summary>
public sealed class DataScopePolicyService
{
    private readonly IHierarchyScopeResolver _hierarchyResolver;
    private readonly IEmployeeScopeDimensionProvider _dimensionProvider;

    public DataScopePolicyService(
        IHierarchyScopeResolver hierarchyResolver,
        IEmployeeScopeDimensionProvider dimensionProvider)
    {
        _hierarchyResolver = hierarchyResolver;
        _dimensionProvider = dimensionProvider;
    }

    /// <summary>
    /// Get scope policy for a user with multiple roles.
    /// Combines rules from all roles using OR logic.
    ///
    /// Example:
    /// - Role A grants Department scope for [D1, D2]
    /// - Role B grants Position scope for [P9]
    /// - Result: Policy.Or(Department([D1,D2]), Position([P9]))
    /// </summary>
    public async Task<DataScopePolicy> GetScopePolicyAsync(
        Guid employeeId,
        IEnumerable<DataScopeLevel> grantedLevels,
        CancellationToken cancellationToken = default)
    {
        var rules = new List<DataScopeRule>();
        var dimensions = await _dimensionProvider.GetScopeDimensionIdsAsync(employeeId, cancellationToken);

        foreach (var level in grantedLevels.Distinct())
        {
            DataScopeRule? rule = level switch
            {
                DataScopeLevel.Global => DataScopeRule.Global(),
                DataScopeLevel.Self => DataScopeRule.Self(employeeId),
                DataScopeLevel.DirectReports => await ResolveDirectReportsAsync(employeeId, cancellationToken),
                DataScopeLevel.EmployeeSet => await ResolveEmployeeSetAsync(employeeId, cancellationToken),
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
                _ => null
            };

            if (rule is not null)
                rules.Add(rule);
        }

        // If any rule is Global, return Global policy
        if (rules.Any(r => r.Level == DataScopeLevel.Global))
            return DataScopePolicy.Or(DataScopeRule.Global());

        // Combine all rules with OR
        return rules.Count > 0
            ? DataScopePolicy.Or(rules.ToArray())
            : DataScopePolicy.Or(DataScopeRule.None());
    }

    private async Task<DataScopeRule> ResolveDirectReportsAsync(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var ids = await _hierarchyResolver.ResolveDirectSubordinatesAsync(
            employeeId, cancellationToken);

        return DataScopeRule.DirectReports(ids);
    }

    private async Task<DataScopeRule> ResolveEmployeeSetAsync(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var subordinateIds = await _hierarchyResolver.ResolveAllSubordinatesAsync(
            employeeId, cancellationToken);

        return DataScopeRule.EmployeeSet(subordinateIds);
    }
}
