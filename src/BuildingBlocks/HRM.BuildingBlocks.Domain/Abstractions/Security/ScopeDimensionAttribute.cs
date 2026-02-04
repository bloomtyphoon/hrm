namespace HRM.BuildingBlocks.Domain.Abstractions.Security;

/// <summary>
/// Marks a property as a scope dimension for data filtering.
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
///     [ScopeDimension(DataScopeLevel.Company)]
///     public Guid? CompanyId { get; private set; }
///
///     [ScopeDimension(DataScopeLevel.Department)]
///     public Guid? DepartmentId { get; private set; }
///
///     [ScopeDimension(DataScopeLevel.Position)]
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
    /// The scope level this property represents.
    /// </summary>
    public DataScopeLevel Level { get; }

    /// <summary>
    /// Create a new scope dimension attribute.
    /// </summary>
    /// <param name="level">The scope level (Company, Department, Position)</param>
    public ScopeDimensionAttribute(DataScopeLevel level)
    {
        // Validate: Only dimension-based levels are allowed
        if (level is DataScopeLevel.None or DataScopeLevel.Self or DataScopeLevel.Global)
        {
            throw new ArgumentException(
                $"ScopeDimension attribute only supports dimension-based levels " +
                $"(Company, Department, Position). Got: {level}",
                nameof(level));
        }

        Level = level;
    }
}
