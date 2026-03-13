namespace HRM.BuildingBlocks.Application.Abstractions.Authorization;

/// <summary>
/// Cross-module contract for resolving employee scope dimensions.
///
/// Implemented by Personnel module (owns employee assignments).
/// Consumed by the shared DataScopeService (BuildingBlocks.Infrastructure).
///
/// All modules resolve data scope from the same source: employee assignments.
/// This interface abstracts that source so the shared DataScopeService
/// doesn't depend on any module directly.
/// </summary>
public interface IEmployeeScopeDimensionProvider
{
    /// <summary>
    /// Get all scope dimension IDs for an employee.
    /// Returns company, department, position, country, and region IDs
    /// from the employee's active assignments.
    /// </summary>
    Task<ScopeDimensionIds> GetScopeDimensionIdsAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Aggregated scope dimension IDs for an employee.
/// Used by the shared DataScopeService to build DataScopeRule.
/// </summary>
public sealed record ScopeDimensionIds
{
    public required IReadOnlyCollection<Guid> CompanyIds { get; init; }
    public required IReadOnlyCollection<Guid> DepartmentIds { get; init; }
    public required IReadOnlyCollection<Guid> PositionIds { get; init; }
    public IReadOnlyCollection<Guid> CountryIds { get; init; } = Array.Empty<Guid>();
    public IReadOnlyCollection<Guid> RegionIds { get; init; } = Array.Empty<Guid>();

    public static ScopeDimensionIds Empty => new()
    {
        CompanyIds = Array.Empty<Guid>(),
        DepartmentIds = Array.Empty<Guid>(),
        PositionIds = Array.Empty<Guid>(),
        CountryIds = Array.Empty<Guid>(),
        RegionIds = Array.Empty<Guid>()
    };
}
