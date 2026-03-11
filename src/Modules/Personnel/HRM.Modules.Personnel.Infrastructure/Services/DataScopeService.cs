using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Personnel.Application.Abstractions;

namespace HRM.Modules.Personnel.Infrastructure.Services;

/// <summary>
/// Implementation of IDataScopeService for the Personnel module.
///
/// DESIGN: This lives in Personnel module because:
/// - Employee data is Personnel's domain
/// - Scope resolution requires employee assignments (Personnel owns this)
/// - Hierarchy resolution requires employee relationships (Personnel owns this)
///
/// Architecture Flow:
///
/// User Request
///      │
///      ▼
/// Permission Check
///      │
///      ▼
/// Scope Resolution (Identity + Personnel modules)
///      │
///      ├── IScopeGrantProvider (Identity) → returns scope LEVEL
///      ├── IEmployeeAssignmentQuery (Personnel) → returns dimension IDs
///      └── IHierarchyScopeResolver (Personnel) → returns subordinate IDs
///      │
///      ▼
/// DataScopeRule / DataScopePolicy
///      │
///      ▼
/// EfScopeExpressionBuilder.Build(rule/policy)
///      │
///      ▼
/// query.Where(expression)
///
/// Flow:
/// 1. Receives userId and permission from caller
/// 2. Gets user's scope LEVEL from IScopeGrantProvider (Identity)
/// 3. Resolves dimension IDs from employee assignments (Personnel)
/// 4. Resolves hierarchy (subordinates) if scope is EmployeeSet (Personnel)
/// 5. Returns DataScopeRule ready for query filtering
/// </summary>
public sealed class DataScopeService : IDataScopeService
{
    private readonly IScopeGrantProvider _grantProvider;
    private readonly IEmployeeAssignmentQuery _assignmentQuery;
    private readonly IHierarchyScopeResolver _hierarchyResolver;

    public DataScopeService(
        IScopeGrantProvider grantProvider,
        IEmployeeAssignmentQuery assignmentQuery,
        IHierarchyScopeResolver hierarchyResolver)
    {
        _grantProvider = grantProvider;
        _assignmentQuery = assignmentQuery;
        _hierarchyResolver = hierarchyResolver;
    }

    /// <inheritdoc />
    public async Task<DataScopeRule> GetScopeRuleAsync(
        Guid userId,
        PermissionDescriptor permission,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Get scope grant from Identity (determines scope LEVEL)
        var grant = await _grantProvider.GetGrantAsync(userId, permission, cancellationToken);

        // Step 2: Handle system accounts and edge cases
        if (grant.IsSystemAccount || grant.Level == DataScopeLevel.Global)
            return DataScopeRule.Global();

        if (grant.Level == DataScopeLevel.None)
            return DataScopeRule.None();

        if (!grant.EmployeeId.HasValue)
            return DataScopeRule.None();

        var employeeId = grant.EmployeeId.Value;

        // Step 3: Resolve scope MEMBERS based on level
        return grant.Level switch
        {
            DataScopeLevel.Self =>
                DataScopeRule.Self(employeeId),

            DataScopeLevel.DirectReports =>
                await ResolveDirectReportsScopeAsync(employeeId, cancellationToken),

            DataScopeLevel.EmployeeSet =>
                await ResolveEmployeeSetScopeAsync(employeeId, cancellationToken),

            DataScopeLevel.Position =>
                await ResolvePositionScopeAsync(employeeId, cancellationToken),

            DataScopeLevel.Department =>
                await ResolveDepartmentScopeAsync(employeeId, cancellationToken),

            DataScopeLevel.Company =>
                await ResolveCompanyScopeAsync(employeeId, cancellationToken),

            DataScopeLevel.Country =>
                await ResolveCountryScopeAsync(employeeId, cancellationToken),

            DataScopeLevel.Region =>
                await ResolveRegionScopeAsync(employeeId, cancellationToken),

            _ => DataScopeRule.None()
        };
    }

    private async Task<DataScopeRule> ResolveDirectReportsScopeAsync(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        // GetSubordinateIdsAsync with includeIndirect: false returns self + direct reports only
        var ids = await _hierarchyResolver.GetSubordinateIdsAsync(
            employeeId,
            includeIndirect: false,
            cancellationToken);

        return DataScopeRule.DirectReports(ids);
    }

    private async Task<DataScopeRule> ResolveEmployeeSetScopeAsync(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var subordinateIds = await _hierarchyResolver.ResolveAllSubordinatesAsync(
            employeeId,
            cancellationToken);

        return DataScopeRule.EmployeeSet(subordinateIds);
    }

    private async Task<DataScopeRule> ResolvePositionScopeAsync(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var positionIds = await _assignmentQuery.GetEmployeePositionIdsAsync(employeeId, cancellationToken);

        return positionIds.Count > 0
            ? DataScopeRule.Position(positionIds)
            : DataScopeRule.None();
    }

    private async Task<DataScopeRule> ResolveDepartmentScopeAsync(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var departmentIds = await _assignmentQuery.GetEmployeeDepartmentIdsAsync(employeeId, cancellationToken);

        return departmentIds.Count > 0
            ? DataScopeRule.Department(departmentIds)
            : DataScopeRule.None();
    }

    private async Task<DataScopeRule> ResolveCompanyScopeAsync(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var companyIds = await _assignmentQuery.GetEmployeeCompanyIdsAsync(employeeId, cancellationToken);

        return companyIds.Count > 0
            ? DataScopeRule.Company(companyIds)
            : DataScopeRule.None();
    }

    private async Task<DataScopeRule> ResolveCountryScopeAsync(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var countryIds = await _assignmentQuery.GetEmployeeCountryIdsAsync(employeeId, cancellationToken);

        return countryIds.Count > 0
            ? DataScopeRule.Country(countryIds)
            : DataScopeRule.None();
    }

    private async Task<DataScopeRule> ResolveRegionScopeAsync(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var regionIds = await _assignmentQuery.GetEmployeeRegionIdsAsync(employeeId, cancellationToken);

        return regionIds.Count > 0
            ? DataScopeRule.Region(regionIds)
            : DataScopeRule.None();
    }
}

/// <summary>
/// Extended data scope service supporting DataScopePolicy for multi-rule scenarios.
/// </summary>
public sealed class DataScopePolicyService
{
    private readonly IHierarchyScopeResolver _hierarchyResolver;
    private readonly IEmployeeAssignmentQuery _assignmentQuery;

    public DataScopePolicyService(
        IHierarchyScopeResolver hierarchyResolver,
        IEmployeeAssignmentQuery assignmentQuery)
    {
        _hierarchyResolver = hierarchyResolver;
        _assignmentQuery = assignmentQuery;
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
        var dimensions = await _assignmentQuery.GetScopeDimensionIdsAsync(employeeId, cancellationToken);

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
        var ids = await _hierarchyResolver.GetSubordinateIdsAsync(
            employeeId, includeIndirect: false, cancellationToken);

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
