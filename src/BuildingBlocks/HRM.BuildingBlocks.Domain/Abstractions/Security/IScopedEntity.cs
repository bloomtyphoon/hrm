namespace HRM.BuildingBlocks.Domain.Abstractions.Security;

/// <summary>
/// Marker interface for entities that support data scoping.
///
/// DESIGN PRINCIPLE:
/// BB does NOT know about organization structure (Company, Department, Position).
/// It only knows about abstract dimensions via ScopeDimensionAttribute.
///
/// Entity implementations use [ScopeDimension] attribute on properties
/// to declare which scope level each property represents.
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
///
/// EfScopeExpressionBuilder discovers dimension properties at startup
/// via reflection and caches the selectors for runtime use.
/// </summary>
public interface IScopedEntity : IOwnedEntity
{
    // Id and OwnerId inherited from IOwnedEntity
}

/// <summary>
/// Interface for entities owned by a specific user.
/// Simpler than IScopedEntity - only supports Self scope.
///
/// Use this for entities that don't have org hierarchy dimensions
/// but still need owner-based filtering.
///
/// Examples: UserSettings, PersonalNotes, SavedFilters
/// </summary>
public interface IOwnedEntity
{
    /// <summary>
    /// Entity's primary key.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Owner of this entity.
    /// </summary>
    Guid OwnerId { get; }
}
