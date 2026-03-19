using HRM.BuildingBlocks.Domain.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.BuildingBlocks.Domain.Entities;

namespace HRM.Modules.Organization.Domain.Entities;

/// <summary>
/// Company aggregate root - represents a legal entity in the organization.
///
/// Companies are the top-level organizational unit.
/// Multi-company support allows employees to have assignments across companies.
///
/// Scope:
/// - [ScopeDimension(Company)] = self-reference for company-level filtering
/// - OwnerId = null (companies are system-owned, not employee-owned)
/// </summary>
public class Company : AuditableEntity, IAggregateRoot, IScopedEntity, ITenantEntity
{
    /// <summary>
    /// Tenant this company belongs to.
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Company code (unique identifier for external systems).
    /// </summary>
    public string Code { get; private set; } = null!;

    /// <summary>
    /// Company name.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Tax identification number.
    /// </summary>
    public string? TaxId { get; private set; }

    /// <summary>
    /// Company status (active, inactive, etc.).
    /// </summary>
    public CompanyStatus Status { get; private set; }

    /// <summary>
    /// Company self-reference for scope filtering.
    /// Entities with this CompanyId are visible to users with Company scope.
    /// </summary>
    [ScopeDimension(DimensionKeys.Company)]
    public Guid CompanyId => Id;

    /// <summary>
    /// Companies are system-owned, not employee-owned.
    /// Returns Guid.Empty to indicate system ownership.
    /// </summary>
    public Guid OwnerId => Guid.Empty;

    // EF Core constructor
    private Company() { }

    /// <summary>
    /// Create a new company belonging to a tenant.
    /// </summary>
    public static Company Create(Guid tenantId, string code, string name, string? taxId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Company code is required", nameof(code));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Company name is required", nameof(name));

        return new Company
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            TaxId = taxId?.Trim(),
            Status = CompanyStatus.Active
        };
    }

    /// <summary>
    /// Update company details.
    /// </summary>
    public void Update(string name, string? taxId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Company name is required", nameof(name));

        Name = name.Trim();
        TaxId = taxId?.Trim();
        MarkAsModified();
    }

    /// <summary>
    /// Activate the company.
    /// </summary>
    public void Activate()
    {
        Status = CompanyStatus.Active;
        MarkAsModified();
    }

    /// <summary>
    /// Deactivate the company.
    /// </summary>
    public void Deactivate()
    {
        Status = CompanyStatus.Inactive;
        MarkAsModified();
    }
}

/// <summary>
/// Company status enumeration.
/// </summary>
public enum CompanyStatus
{
    /// <summary>Company is active and operational.</summary>
    Active = 1,

    /// <summary>Company is inactive (no new operations).</summary>
    Inactive = 2,

    /// <summary>Company is pending setup.</summary>
    Pending = 3
}
