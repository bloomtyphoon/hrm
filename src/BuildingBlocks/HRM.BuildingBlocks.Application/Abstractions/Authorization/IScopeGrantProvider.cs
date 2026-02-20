using HRM.BuildingBlocks.Domain.Abstractions.Security;

namespace HRM.BuildingBlocks.Application.Abstractions.Authorization;

/// <summary>
/// Contract for providing scope grants.
///
/// Implemented by Identity module - provides user's scope LEVEL.
/// Consumed by Organization module - uses level to resolve MEMBERS.
///
/// Design (no circular dependency):
/// - BuildingBlocks: IScopeGrantProvider contract
/// - Identity: Implements IScopeGrantProvider
/// - Organization: Consumes IScopeGrantProvider + implements IDataScopeService
///
/// Flow:
/// 1. Query handler calls IDataScopeService.GetScopeRuleAsync(userId, permission)
/// 2. DataScopeService (Org) calls IScopeGrantProvider.GetGrantAsync (Identity)
/// 3. Identity returns ScopeGrant (level + employeeId)
/// 4. Organization resolves members based on level (hierarchy, dimensions)
/// 5. Returns DataScopeRule ready for query filtering
/// </summary>
public interface IScopeGrantProvider
{
    /// <summary>
    /// Get the scope grant for a user and permission.
    /// Returns the scope LEVEL (what type of access) without resolving MEMBERS.
    /// </summary>
    Task<ScopeGrant> GetGrantAsync(
        Guid userId,
        string permission,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Scope grant from Identity module.
/// Contains the scope level and user context needed for resolution.
/// </summary>
public sealed record ScopeGrant
{
    /// <summary>
    /// Scope level granted for this permission.
    /// </summary>
    public required DataScopeLevel Level { get; init; }

    /// <summary>
    /// Employee ID (if user is an employee).
    /// Null for system accounts.
    /// </summary>
    public Guid? EmployeeId { get; init; }

    /// <summary>
    /// Whether this is a system account (operators have global access).
    /// </summary>
    public bool IsSystemAccount { get; init; }

    /// <summary>
    /// Grant for system accounts (global access).
    /// </summary>
    public static ScopeGrant SystemAccount => new()
    {
        Level = DataScopeLevel.Global,
        IsSystemAccount = true
    };

    /// <summary>
    /// No access grant.
    /// </summary>
    public static ScopeGrant NoAccess => new()
    {
        Level = DataScopeLevel.None
    };

    /// <summary>
    /// Create a grant for an employee.
    /// </summary>
    public static ScopeGrant ForEmployee(Guid employeeId, DataScopeLevel level) => new()
    {
        Level = level,
        EmployeeId = employeeId,
        IsSystemAccount = false
    };
}
