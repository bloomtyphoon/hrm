using HRM.BuildingBlocks.Domain.Entities;
using HRM.Modules.Identity.Domain.Events;

namespace HRM.Modules.Identity.Domain.Entities;

/// <summary>
/// Profile data for System accounts (operators, admins)
/// Contains role and permission information
///
/// Relationship:
/// - Account (1) --- (0..1) SystemProfile
/// - Only for AccountType.System
///
/// This separates system-specific data from the authentication entity.
/// </summary>
public class SystemProfile : AuditableEntity
{
    /// <summary>
    /// Reference to the Account
    /// </summary>
    public Guid AccountId { get; private set; }

    /// <summary>
    /// Whether this is a super administrator (bypasses all permission checks)
    /// </summary>
    public bool IsSuperAdmin { get; private set; }

    /// <summary>
    /// Department/team this operator belongs to (optional)
    /// Used for organizational purposes, not scoping
    /// </summary>
    public string? Department { get; private set; }

    /// <summary>
    /// Job title (optional)
    /// </summary>
    public string? JobTitle { get; private set; }

    /// <summary>
    /// Notes about this operator (admin-only)
    /// </summary>
    public string? Notes { get; private set; }

    // Audit fields inherited from AuditableEntity:
    // - CreatedAtUtc, ModifiedAtUtc, CreatedById, ModifiedById

    // Navigation property (configured in EF)
    // public Account Account { get; private set; } = null!;
    // public ICollection<SystemProfileRole> Roles { get; private set; } = new List<SystemProfileRole>();

    // Private constructor for EF
    private SystemProfile() { }

    /// <summary>
    /// Create a new system profile
    /// </summary>
    public static SystemProfile Create(
        Guid accountId,
        bool isSuperAdmin = false,
        string? department = null,
        string? jobTitle = null)
    {
        var profile = new SystemProfile
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            IsSuperAdmin = isSuperAdmin,
            Department = department,
            JobTitle = jobTitle
            // CreatedAtUtc is set automatically by AuditableEntity constructor
        };

        profile.AddDomainEvent(new SystemProfileCreatedDomainEvent(
            profile.Id, accountId, isSuperAdmin));

        return profile;
    }

    /// <summary>
    /// Grant super admin privileges (idempotent - no-op if already super admin)
    /// </summary>
    public void GrantSuperAdmin()
    {
        if (IsSuperAdmin)
            return;

        IsSuperAdmin = true;
        MarkAsModified();

        AddDomainEvent(new SuperAdminGrantedDomainEvent(Id, AccountId));
    }

    /// <summary>
    /// Revoke super admin privileges (idempotent - no-op if not super admin)
    /// </summary>
    public void RevokeSuperAdmin()
    {
        if (!IsSuperAdmin)
            return;

        IsSuperAdmin = false;
        MarkAsModified();

        AddDomainEvent(new SuperAdminRevokedDomainEvent(Id, AccountId));
    }

    /// <summary>
    /// Update profile information
    /// </summary>
    public void Update(string? department, string? jobTitle, string? notes)
    {
        Department = department;
        JobTitle = jobTitle;
        Notes = notes;
        MarkAsModified();

        AddDomainEvent(new SystemProfileUpdatedDomainEvent(
            Id, AccountId, Department, JobTitle));
    }
}
