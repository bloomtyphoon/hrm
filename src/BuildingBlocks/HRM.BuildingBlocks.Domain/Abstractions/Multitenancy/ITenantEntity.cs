namespace HRM.BuildingBlocks.Domain.Abstractions.Multitenancy;

/// <summary>
/// Marker interface for entities that are scoped to a specific tenant.
/// All entities implementing this interface will be automatically filtered
/// by the current tenant via EF Core global query filters in ModuleDbContext.
///
/// TenantId is always a non-nullable Guid:
/// - Customer entities: TenantId = customer tenant Guid
/// - System entities: TenantId = WellKnownTenants.SystemTenantId
///
/// Never use Guid.Empty or null to represent "system" — use WellKnownTenants.SystemTenantId.
/// </summary>
public interface ITenantEntity
{
    /// <summary>
    /// The tenant this entity belongs to.
    /// Must be WellKnownTenants.SystemTenantId for system-level data.
    /// </summary>
    Guid TenantId { get; }
}
