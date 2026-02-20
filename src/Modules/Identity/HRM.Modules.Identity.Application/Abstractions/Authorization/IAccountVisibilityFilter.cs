namespace HRM.Modules.Identity.Application.Abstractions.Authorization;

/// <summary>
/// Provides account visibility filtering based on the current user's context.
///
/// Rules:
/// - System accounts: see all accounts (returns null = no filter)
/// - Employee accounts: see only accounts in their assigned companies
///   (determined via EmployeeAssignments cross-module query)
/// </summary>
public interface IAccountVisibilityFilter
{
    /// <summary>
    /// Get the set of AccountIds visible to the current user.
    /// Returns null if the user has unrestricted visibility (System accounts).
    /// Returns empty set if the user has no visibility (no assignments).
    /// </summary>
    Task<HashSet<Guid>?> GetVisibleAccountIdsAsync(CancellationToken cancellationToken = default);
}
