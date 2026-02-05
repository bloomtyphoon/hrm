using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Organization.Application.Commands.CreateCompany;

/// <summary>
/// Command to create a new company.
///
/// CQRS Pattern:
/// - Command: Mutates system state (creates new company)
/// - Returns: Company ID on success, Error on failure (Result pattern)
/// - Idempotent: No (duplicate code returns error)
///
/// Validation:
/// - Code: 1-50 chars, alphanumeric with underscores/hyphens, unique
/// - Name: 1-200 chars, required
/// - TaxId: Optional, max 50 chars
///
/// Business Rules:
/// - Company created in Active status by default
/// - Code is normalized to uppercase
/// - Domain event raised: CompanyCreatedDomainEvent (future)
///
/// Flow:
/// 1. Validate command (FluentValidation)
/// 2. Check code uniqueness
/// 3. Create Company aggregate via Company.Create()
/// 4. Save to database (UnitOfWork)
/// 5. Return company ID
/// </summary>
/// <param name="Code">Company code (1-50 chars, alphanumeric with underscores/hyphens, unique)</param>
/// <param name="Name">Company name (1-200 chars, required)</param>
/// <param name="TaxId">Tax identification number (optional, max 50 chars)</param>
public sealed record CreateCompanyCommand(
    string Code,
    string Name,
    string? TaxId = null
) : IModuleCommand<Guid>
{
    /// <summary>
    /// Module name for Unit of Work routing.
    /// Commands in Organization module always commit via OrganizationDbContext.
    /// </summary>
    public string ModuleName => "Organization";
}
