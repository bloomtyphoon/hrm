namespace HRM.BuildingBlocks.Application.Abstractions.Multitenancy;

/// <summary>
/// Provides the current request's tenant context.
/// Implemented by CurrentUserService in the Identity module.
///
/// Design:
/// - TenantId is Guid? — null only when claim is absent (anonymous/background service)
/// - WellKnownTenants.SystemTenantId → system tenant, bypasses query filters
/// - Customer tenant Guid → filtered access to that tenant's data only
/// - Null → used by background services (no HTTP context), no filtering applied
///
/// Registration: scoped per-request via DI, resolved from JWT claims.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Current tenant ID extracted from JWT "TenantId" claim.
    /// Null when there is no authenticated user (background services, anonymous).
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    /// True when TenantId is present in the current request context.
    /// False for background services and anonymous requests.
    /// </summary>
    bool HasTenant { get; }
}
