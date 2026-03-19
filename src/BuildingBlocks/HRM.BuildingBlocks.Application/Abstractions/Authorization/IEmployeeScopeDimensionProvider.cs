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
    /// Returns dimension IDs keyed by dimension key (e.g., "Company", "Department")
    /// from the employee's active assignments.
    /// </summary>
    Task<ScopeDimensionIds> GetScopeDimensionIdsAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Aggregated scope dimension IDs for an employee.
/// Dictionary-based: supports dynamic dimension keys from DB.
/// Used by the shared DataScopeService to build DataScopeRule.
/// </summary>
public sealed class ScopeDimensionIds
{
    private readonly Dictionary<string, IReadOnlyCollection<Guid>> _dimensions;

    public ScopeDimensionIds(Dictionary<string, IReadOnlyCollection<Guid>> dimensions)
    {
        _dimensions = dimensions ?? throw new ArgumentNullException(nameof(dimensions));
    }

    /// <summary>
    /// Get dimension IDs by dimension key.
    /// Returns empty collection if dimension key not found.
    /// </summary>
    public IReadOnlyCollection<Guid> GetIds(string dimensionKey)
        => _dimensions.TryGetValue(dimensionKey, out var ids) ? ids : Array.Empty<Guid>();

    /// <summary>All dimension keys that have IDs.</summary>
    public IReadOnlyCollection<string> DimensionKeys => _dimensions.Keys;

    // Backward-compatible convenience properties
    public IReadOnlyCollection<Guid> CompanyIds => GetIds("Company");
    public IReadOnlyCollection<Guid> DepartmentIds => GetIds("Department");
    public IReadOnlyCollection<Guid> PositionIds => GetIds("Position");
    public IReadOnlyCollection<Guid> CountryIds => GetIds("Country");
    public IReadOnlyCollection<Guid> RegionIds => GetIds("Region");

    public static ScopeDimensionIds Empty => new(new Dictionary<string, IReadOnlyCollection<Guid>>());

    /// <summary>Builder for constructing ScopeDimensionIds.</summary>
    public sealed class Builder
    {
        private readonly Dictionary<string, IReadOnlyCollection<Guid>> _dimensions = new(StringComparer.OrdinalIgnoreCase);

        public Builder Add(string dimensionKey, IReadOnlyCollection<Guid> ids)
        {
            if (ids.Count > 0)
                _dimensions[dimensionKey] = ids;
            return this;
        }

        public ScopeDimensionIds Build() => new(_dimensions);
    }
}
