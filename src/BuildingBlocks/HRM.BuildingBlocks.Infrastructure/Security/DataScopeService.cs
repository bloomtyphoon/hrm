using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Domain.Abstractions.Security;

namespace HRM.BuildingBlocks.Infrastructure.Security;

/// <summary>
/// Shared implementation of IDataScopeService for all modules.
///
/// All modules resolve data scope from the same source: employee assignments.
/// Uses category-based dispatch for dynamic scope level support.
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

        if (grant.IsSystemAccount || grant.Level.IsGlobal)
            return DataScopeRule.Global();

        if (grant.Level.IsNone)
            return DataScopeRule.None();

        if (!grant.EmployeeId.HasValue)
            return DataScopeRule.None();

        var employeeId = grant.EmployeeId.Value;

        // Category-based dispatch
        return grant.Level.Category switch
        {
            ScopeCategory.Set => await ResolveSetScopeAsync(grant.Level, employeeId, cancellationToken),
            ScopeCategory.Dimension => await ResolveDimensionScopeAsync(grant.Level, employeeId, cancellationToken),
            _ => throw new InvalidOperationException(
                $"Unknown ScopeCategory '{grant.Level.Category}' for scope level '{grant.Level.Name}'. " +
                "Ensure all scope categories have handlers registered.")
        };
    }

    /// <inheritdoc />
    public async Task<DataScopeRule> GetCompanyScopeRuleAsync(
        Guid userId,
        PermissionDescriptor permission,
        CancellationToken cancellationToken = default)
    {
        var grant = await _grantProvider.GetGrantAsync(userId, permission, cancellationToken);

        if (grant.IsSystemAccount || grant.Level.IsGlobal)
            return DataScopeRule.Global();

        if (grant.Level.IsNone || !grant.EmployeeId.HasValue)
            return DataScopeRule.None();

        // Always resolve to Company scope regardless of granted level.
        // For Organization queries, entities are scoped by company membership.
        var dimensions = await _dimensionProvider.GetScopeDimensionIdsAsync(
            grant.EmployeeId.Value, cancellationToken);

        return dimensions.CompanyIds.Count > 0
            ? DataScopeRule.Company(dimensions.CompanyIds)
            : DataScopeRule.None();
    }

    private async Task<DataScopeRule> ResolveSetScopeAsync(
        DataScopeLevel level, Guid employeeId, CancellationToken cancellationToken)
    {
        // Self is a special set scope: just the employee's own ID
        if (level == DataScopeLevel.Self)
            return DataScopeRule.Self(employeeId);

        // Resolve employee IDs based on resolution strategy
        var ids = level.ResolutionKey switch
        {
            ResolutionKeys.DirectReports => await _hierarchyResolver.ResolveDirectSubordinatesAsync(employeeId, cancellationToken),
            ResolutionKeys.AllSubordinates => await _hierarchyResolver.ResolveAllSubordinatesAsync(employeeId, cancellationToken),
            _ => (IReadOnlySet<Guid>)new HashSet<Guid> { employeeId } // Unknown strategy: fallback to self
        };

        return DataScopeRule.ForSet(level, ids);
    }

    private async Task<DataScopeRule> ResolveDimensionScopeAsync(
        DataScopeLevel level, Guid employeeId, CancellationToken cancellationToken)
    {
        var dimensions = await _dimensionProvider.GetScopeDimensionIdsAsync(employeeId, cancellationToken);

        if (level.DimensionKey is null)
            return DataScopeRule.None();

        var ids = dimensions.GetIds(level.DimensionKey);

        return ids.Count > 0
            ? DataScopeRule.ForDimension(level, ids)
            : DataScopeRule.None();
    }
}
