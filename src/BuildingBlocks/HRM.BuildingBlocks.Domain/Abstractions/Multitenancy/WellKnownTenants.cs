namespace HRM.BuildingBlocks.Domain.Abstractions.Multitenancy;

/// <summary>
/// Well-known tenant identifiers used across the system.
/// These are stable constants that must never change after deployment.
/// </summary>
public static class WellKnownTenants
{
    /// <summary>
    /// System tenant for platform operators and super admins.
    /// Accounts with this TenantId bypass all tenant query filters.
    ///
    /// Rules:
    /// - Cannot be deleted or deactivated (enforced by Tenant domain rules)
    /// - Cannot have its Code changed
    /// - System accounts always belong to this tenant
    ///
    /// Value: ffffffff-ffff-ffff-ffff-ffffffffffff
    /// </summary>
    public static readonly Guid SystemTenantId = new("ffffffff-ffff-ffff-ffff-ffffffffffff");
}
