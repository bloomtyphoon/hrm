using HRM.BuildingBlocks.Domain.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.BuildingBlocks.Domain.Entities;
using HRM.Modules.Identity.Domain.Events;

namespace HRM.Modules.Identity.Domain.Entities;

/// <summary>
/// Profile data for Employee accounts
/// Links Account to Employee entity and contains scope information
///
/// Relationship:
/// - Account (1) --- (0..1) EmployeeProfile
/// - EmployeeProfile (1) --- (1) Employee (in Personnel module, referenced by ID only)
/// - Only for AccountType.Employee
///
/// This bridges authentication (Account) with HR data (Employee).
///
/// Cross-module reference:
/// - EmployeeId is an opaque Guid reference to Personnel module
/// - Identity module does NOT depend on Personnel.Domain
/// - This follows kgrzybek-style module isolation
/// </summary>
public class EmployeeProfile : AuditableEntity, ITenantEntity
{
    /// <summary>
    /// Tenant this employee profile belongs to (denormalized from Account/Employee).
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Reference to the Account
    /// </summary>
    public Guid AccountId { get; private set; }

    /// <summary>
    /// Reference to the Employee entity (in Personnel module).
    /// Opaque ID — Identity module does not reference Personnel.Domain.
    /// </summary>
    public Guid EmployeeId { get; private set; }

    /// <summary>
    /// Default scope level for this employee's permissions.
    /// Can be overridden per-permission via RolePermissions.
    ///
    /// Uses DataScopeLevel from BuildingBlocks (shared contract).
    /// Other modules receive DataScopeRule, not DataScopeLevel directly.
    /// </summary>
    public DataScopeLevel DefaultScopeLevel { get; private set; } = DataScopeLevel.Self;

    /// <summary>
    /// Primary company ID (for multi-company scenarios)
    /// </summary>
    public Guid? PrimaryCompanyId { get; private set; }

    /// <summary>
    /// Primary department ID
    /// </summary>
    public Guid? PrimaryDepartmentId { get; private set; }

    /// <summary>
    /// Primary position ID
    /// </summary>
    public Guid? PrimaryPositionId { get; private set; }

    /// <summary>
    /// Whether employee can access data across all assigned companies
    /// (vs only primary company)
    /// </summary>
    public bool CanAccessAllAssignedCompanies { get; private set; } = true;

    private readonly List<EmployeeCompanyAccess> _companyAccess = new();

    /// <summary>
    /// All companies this employee has access to.
    /// Denormalized copy from Personnel module's EmployeeAssignments.
    /// Used for account visibility filtering and data scope resolution
    /// without cross-module queries.
    ///
    /// Synced via:
    /// - Admin API when creating/updating EmployeeProfile
    /// - Integration events from Personnel module when assignments change
    /// </summary>
    public IReadOnlyCollection<EmployeeCompanyAccess> CompanyAccess => _companyAccess.AsReadOnly();

    // Private constructor for EF
    private EmployeeProfile() { }

    /// <summary>
    /// Create a new employee profile.
    /// TenantId must match the account's tenant.
    /// </summary>
    public static EmployeeProfile Create(
        Guid tenantId,
        Guid accountId,
        Guid employeeId,
        DataScopeLevel defaultScopeLevel = DataScopeLevel.Self,
        Guid? primaryCompanyId = null,
        Guid? primaryDepartmentId = null,
        Guid? primaryPositionId = null,
        IReadOnlyList<Guid>? companyIds = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        var profile = new EmployeeProfile
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AccountId = accountId,
            EmployeeId = employeeId,
            DefaultScopeLevel = defaultScopeLevel,
            PrimaryCompanyId = primaryCompanyId,
            PrimaryDepartmentId = primaryDepartmentId,
            PrimaryPositionId = primaryPositionId
        };

        // Initialize company access list
        if (companyIds != null)
        {
            foreach (var companyId in companyIds.Distinct())
            {
                profile._companyAccess.Add(EmployeeCompanyAccess.Create(companyId));
            }
        }
        else if (primaryCompanyId.HasValue)
        {
            // Default: at least the primary company
            profile._companyAccess.Add(EmployeeCompanyAccess.Create(primaryCompanyId.Value));
        }

        profile.AddDomainEvent(new EmployeeProfileCreatedDomainEvent(
            profile.Id, accountId, employeeId));

        return profile;
    }

    /// <summary>
    /// Update primary assignments
    /// </summary>
    public void UpdatePrimaryAssignments(
        Guid? companyId,
        Guid? departmentId,
        Guid? positionId)
    {
        PrimaryCompanyId = companyId;
        PrimaryDepartmentId = departmentId;
        PrimaryPositionId = positionId;
        MarkAsModified();

        AddDomainEvent(new EmployeeProfileUpdatedDomainEvent(
            Id, AccountId, EmployeeId));
    }

    /// <summary>
    /// Update default scope level
    /// </summary>
    public void UpdateDefaultScopeLevel(DataScopeLevel scopeLevel)
    {
        if (DefaultScopeLevel == scopeLevel)
            return;

        DefaultScopeLevel = scopeLevel;
        MarkAsModified();

        AddDomainEvent(new EmployeeProfileUpdatedDomainEvent(
            Id, AccountId, EmployeeId));
    }

    /// <summary>
    /// Sync company access list.
    /// Replaces current list with the provided company IDs.
    /// Called when Personnel module notifies about assignment changes,
    /// or when admin updates the employee profile.
    /// </summary>
    public void SyncCompanyAccess(IReadOnlyList<Guid> companyIds)
    {
        _companyAccess.Clear();
        foreach (var companyId in companyIds.Distinct())
        {
            _companyAccess.Add(EmployeeCompanyAccess.Create(companyId));
        }
        MarkAsModified();
    }

    /// <summary>
    /// Set whether employee can access all assigned companies
    /// </summary>
    public void SetCanAccessAllAssignedCompanies(bool value)
    {
        if (CanAccessAllAssignedCompanies == value)
            return;

        CanAccessAllAssignedCompanies = value;
        MarkAsModified();

        AddDomainEvent(new EmployeeProfileUpdatedDomainEvent(
            Id, AccountId, EmployeeId));
    }
}
