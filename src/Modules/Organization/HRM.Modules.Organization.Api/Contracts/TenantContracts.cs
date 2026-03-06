namespace HRM.Modules.Organization.Api.Contracts;

/// <summary>
/// Response DTO for tenant operations.
/// </summary>
public sealed record TenantResponse(
    Guid Id,
    string Code,
    string Name,
    string Status,
    bool IsSystemTenant,
    string? Subdomain,
    DateTime CreatedAtUtc,
    DateTime? ModifiedAtUtc
);

/// <summary>
/// Request DTO for creating a tenant.
/// </summary>
public sealed record CreateTenantRequest(string Code, string Name, string? Subdomain = null);

/// <summary>
/// Request DTO for updating a tenant.
/// </summary>
public sealed record UpdateTenantRequest(string Name, string? Subdomain = null);

/// <summary>
/// Lightweight response for public subdomain resolution.
/// </summary>
public sealed record TenantSubdomainResponse(Guid Id, string Code, string Name, string Status);
