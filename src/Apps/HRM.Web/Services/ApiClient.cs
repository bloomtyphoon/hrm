using HRM.Web.Services.Abstractions;

namespace HRM.Web.Services;

/// <summary>
/// Aggregate API client facade providing access to all module clients.
///
/// This is a convenience wrapper for scenarios requiring cross-module access
/// (e.g., Admin dashboard, orchestration layers).
///
/// For single-module access, prefer injecting the specific module client directly:
/// - IIdentityApiClient for auth/account operations
/// - IOrganizationApiClient for company/department/position operations
/// - IPersonnelApiClient for employee operations
/// </summary>
public sealed class ApiClient : IApiClient
{
    /// <inheritdoc />
    public IIdentityApiClient Identity { get; }

    /// <inheritdoc />
    public IOrganizationApiClient Organization { get; }

    /// <inheritdoc />
    public IPersonnelApiClient Personnel { get; }

    public ApiClient(
        IIdentityApiClient identity,
        IOrganizationApiClient organization,
        IPersonnelApiClient personnel)
    {
        Identity = identity;
        Organization = organization;
        Personnel = personnel;
    }
}
