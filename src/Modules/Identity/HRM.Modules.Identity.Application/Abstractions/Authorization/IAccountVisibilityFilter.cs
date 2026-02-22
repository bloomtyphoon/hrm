namespace HRM.Modules.Identity.Application.Abstractions.Authorization;

/// <summary>
/// Provides account visibility filtering based on the current user's context.
/// Used by command handlers and single-resource query handlers for access checks.
///
/// For list queries (GetAccounts), use IDataScopeService directly — it produces an EF subquery
/// that avoids loading account IDs into memory. This interface is retained for single-account
/// access checks where loading a bounded HashSet is acceptable.
///
/// Rules:
/// - System accounts: see all accounts (returns null = no filter)
/// - Employee accounts: see only accounts in their accessible companies
/// </summary>
public interface IAccountVisibilityFilter
{
    /// <summary>
    /// Get the set of AccountIds visible to the current user.
    /// Returns null if the user has unrestricted visibility (System accounts).
    /// Returns empty set if the user has no visibility.
    /// </summary>
    Task<HashSet<Guid>?> GetVisibleAccountIdsAsync(CancellationToken cancellationToken = default);
}
