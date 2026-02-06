namespace HRM.Web.Services.Abstractions;

/// <summary>
/// Aggregate API client providing access to all module clients.
/// Use this when cross-module access is needed (e.g., Admin dashboard).
///
/// For single-module access, prefer injecting the specific module client:
/// - IIdentityApiClient for auth/account operations
/// - IOrganizationApiClient for company operations
/// </summary>
public interface IApiClient
{
    /// <summary>
    /// Identity module client for auth, accounts, and sessions.
    /// </summary>
    IIdentityApiClient Identity { get; }

    /// <summary>
    /// Organization module client for company management.
    /// </summary>
    IOrganizationApiClient Organization { get; }
}
