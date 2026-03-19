namespace HRM.BuildingBlocks.Domain.Abstractions.Security;

/// <summary>
/// Marks a property as a scope dimension for data filtering.
///
/// Uses a string key (e.g., "Company", "Department") that matches
/// DataScopeLevel.DimensionKey from the DB-driven scope level definitions.
///
/// BB does NOT know what Company/Department/Position are.
/// It only knows: "This property represents dimension X".
///
/// The Organization/Personnel module maps its concrete org fields
/// to abstract dimensions using this attribute.
///
/// Usage:
/// <code>
/// public class Employee : Entity, IScopedEntity
/// {
///     [ScopeDimension("Company")]
///     public Guid? CompanyId { get; private set; }
///
///     [ScopeDimension("Department")]
///     public Guid? DepartmentId { get; private set; }
///
///     [ScopeDimension("Position")]
///     public Guid? PositionId { get; private set; }
///
///     public Guid OwnerId => Id;
/// }
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class ScopeDimensionAttribute : Attribute
{
    /// <summary>
    /// The dimension key this property represents.
    /// Must match DataScopeLevel.DimensionKey for the corresponding scope level.
    /// </summary>
    public string DimensionKey { get; }

    /// <summary>
    /// Create a new scope dimension attribute.
    /// </summary>
    /// <param name="dimensionKey">The dimension key (e.g., "Company", "Department", "Position")</param>
    public ScopeDimensionAttribute(string dimensionKey)
    {
        if (string.IsNullOrWhiteSpace(dimensionKey))
            throw new ArgumentException("Dimension key cannot be null or empty.", nameof(dimensionKey));

        DimensionKey = dimensionKey;
    }
}
