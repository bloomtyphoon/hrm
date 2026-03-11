using HRM.BuildingBlocks.Application.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Domain.Abstractions.Multitenancy;

namespace HRM.Modules.Personnel.Tests.Helpers;

/// <summary>
/// Fake tenant context for integration tests.
/// Simulates a specific customer tenant or the system tenant.
/// </summary>
internal sealed class FakeTenantContext : ITenantContext
{
    public FakeTenantContext(Guid tenantId)
    {
        TenantId = tenantId;
    }

    public static FakeTenantContext SystemTenant =>
        new(WellKnownTenants.SystemTenantId);

    public static FakeTenantContext ForTenant(Guid tenantId) =>
        new(tenantId);

    public Guid? TenantId { get; }
    public bool HasTenant => TenantId.HasValue;
}
