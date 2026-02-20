namespace HRM.Modules.Organization.Application.DTOs;

public sealed record CompanyDto(
    Guid Id,
    string Code,
    string Name,
    string? TaxId,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? ModifiedAtUtc
);
