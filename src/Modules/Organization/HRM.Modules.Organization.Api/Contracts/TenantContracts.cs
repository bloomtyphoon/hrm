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
    DateTime CreatedAtUtc,
    DateTime? ModifiedAtUtc
);

/// <summary>
/// Request DTO for creating a tenant.
/// </summary>
public sealed record CreateTenantRequest(string Code, string Name);

/// <summary>
/// Request DTO for updating a tenant.
/// </summary>
public sealed record UpdateTenantRequest(string Name);
