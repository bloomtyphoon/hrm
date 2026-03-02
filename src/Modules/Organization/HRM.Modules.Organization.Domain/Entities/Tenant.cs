using HRM.BuildingBlocks.Domain.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Domain.Entities;

namespace HRM.Modules.Organization.Domain.Entities;

/// <summary>
/// Tenant aggregate root — the top-level SaaS boundary.
///
/// A Tenant represents one customer organization (e.g., a corporate group).
/// Each Tenant can contain multiple Companies (subsidiaries/branches).
///
/// System Tenant:
/// - Id = WellKnownTenants.SystemTenantId (ffffffff-ffff-ffff-ffff-ffffffffffff)
/// - IsSystemTenant = true
/// - Cannot be deleted, deactivated, or have its Code changed
/// - Owned by the platform operator
///
/// Guard rules enforced in domain methods:
/// - System tenant: immutable Code, cannot suspend/deactivate/delete
/// </summary>
public class Tenant : AuditableEntity, IAggregateRoot
{
    /// <summary>
    /// Short unique identifier (e.g., "ACME", "SYSTEM").
    /// Immutable for system tenant.
    /// </summary>
    public string Code { get; private set; } = null!;

    /// <summary>
    /// Display name of the tenant.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Current tenant status.
    /// </summary>
    public TenantStatus Status { get; private set; }

    /// <summary>
    /// True only for the platform system tenant (WellKnownTenants.SystemTenantId).
    /// Prevents accidental deletion, deactivation, or code changes.
    /// </summary>
    public bool IsSystemTenant { get; private set; }

    // EF Core constructor
    private Tenant() { }

    /// <summary>
    /// Create a new customer tenant.
    /// </summary>
    public static Tenant Create(string code, string name)
    {
        ValidateCode(code);
        ValidateName(name);

        return new Tenant
        {
            Id = Guid.NewGuid(),
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Status = TenantStatus.Active,
            IsSystemTenant = false
        };
    }

    /// <summary>
    /// Create the system tenant with a fixed well-known ID.
    /// Called only during initial database seeding.
    /// </summary>
    public static Tenant CreateSystem()
    {
        return new Tenant
        {
            Id = WellKnownTenants.SystemTenantId,
            Code = "SYSTEM",
            Name = "System Tenant",
            Status = TenantStatus.Active,
            IsSystemTenant = true
        };
    }

    /// <summary>
    /// Update tenant name.
    /// </summary>
    public void Update(string name)
    {
        ValidateName(name);
        Name = name.Trim();
        MarkAsModified();
    }

    /// <summary>
    /// Update tenant code. Not allowed for system tenant.
    /// </summary>
    public void UpdateCode(string code)
    {
        if (IsSystemTenant)
            throw new InvalidOperationException("System tenant code cannot be changed.");

        ValidateCode(code);
        Code = code.Trim().ToUpperInvariant();
        MarkAsModified();
    }

    /// <summary>
    /// Suspend the tenant (temporarily block access).
    /// </summary>
    public void Suspend()
    {
        if (IsSystemTenant)
            throw new InvalidOperationException("System tenant cannot be suspended.");

        if (Status == TenantStatus.Suspended) return;

        Status = TenantStatus.Suspended;
        MarkAsModified();
    }

    /// <summary>
    /// Reactivate a suspended tenant.
    /// </summary>
    public void Activate()
    {
        if (Status == TenantStatus.Active) return;
        Status = TenantStatus.Active;
        MarkAsModified();
    }

    /// <summary>
    /// Deactivate the tenant (permanent, data retained).
    /// </summary>
    public void Deactivate()
    {
        if (IsSystemTenant)
            throw new InvalidOperationException("System tenant cannot be deactivated.");

        if (Status == TenantStatus.Deactivated) return;

        Status = TenantStatus.Deactivated;
        MarkAsModified();
    }

    private static void ValidateCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Tenant code is required.", nameof(code));
        if (code.Trim().Length > 50)
            throw new ArgumentException("Tenant code must not exceed 50 characters.", nameof(code));
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tenant name is required.", nameof(name));
        if (name.Trim().Length > 200)
            throw new ArgumentException("Tenant name must not exceed 200 characters.", nameof(name));
    }
}

/// <summary>
/// Tenant lifecycle status.
/// </summary>
public enum TenantStatus
{
    /// <summary>Tenant is active and operational.</summary>
    Active = 1,

    /// <summary>Tenant is temporarily suspended.</summary>
    Suspended = 2,

    /// <summary>Tenant is permanently deactivated.</summary>
    Deactivated = 3
}
