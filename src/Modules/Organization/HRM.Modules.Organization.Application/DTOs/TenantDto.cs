namespace HRM.Modules.Organization.Application.DTOs;

public sealed record TenantDto(
    Guid Id,
    string Code,
    string Name,
    string Status,
    bool IsSystemTenant,
    DateTime CreatedAtUtc,
    DateTime? ModifiedAtUtc
);
