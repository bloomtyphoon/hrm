namespace HRM.Modules.Organization.Api.Contracts;

/// <summary>
/// Response DTO for company operations.
/// Maps from Company entity.
///
/// Used By:
/// - POST /api/organization/companies (201 Created)
/// - GET /api/organization/companies/{id} (200 OK) - future
/// - GET /api/organization/companies (200 OK) - future
///
/// Status Values:
/// - "Active": Company is operational
/// - "Inactive": Company is inactive
/// - "Pending": Company is pending setup
/// </summary>
/// <param name="Id">Company ID</param>
/// <param name="Code">Company code (unique identifier)</param>
/// <param name="Name">Company name</param>
/// <param name="TaxId">Tax identification number (optional)</param>
/// <param name="Status">Company status (Active, Inactive, Pending)</param>
/// <param name="CreatedAtUtc">Timestamp when created</param>
/// <param name="ModifiedAtUtc">Timestamp when last modified (null if never modified)</param>
public sealed record CompanyResponse(
    Guid Id,
    string Code,
    string Name,
    string? TaxId,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? ModifiedAtUtc
);

/// <summary>
/// Request DTO for creating a company.
/// </summary>
/// <param name="Code">Company code (1-50 chars, alphanumeric with underscores/hyphens)</param>
/// <param name="Name">Company name (1-200 chars)</param>
/// <param name="TaxId">Tax identification number (optional, max 50 chars)</param>
public sealed record CreateCompanyRequest(
    string Code,
    string Name,
    string? TaxId = null
);
