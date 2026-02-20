using HRM.BuildingBlocks.Domain.Abstractions.Results;

namespace HRM.Modules.Organization.Domain.Errors;

/// <summary>
/// Static error definitions for Company operations.
/// Uses pure DomainError types - no HTTP concerns.
///
/// Error Categories:
/// - ConflictError: Resource already exists (code)
/// - NotFoundError: Company not found by ID/code
/// - ValidationError: Business rule violations
///
/// HTTP Mapping:
/// Mapping to HTTP status codes happens in API layer.
/// This keeps Domain pure and transport-agnostic.
/// </summary>
public static class CompanyErrors
{
    /// <summary>
    /// Company code already exists in the system.
    /// Maps to HTTP 409 Conflict in API layer.
    /// </summary>
    public static ConflictError CodeAlreadyExists(string code) =>
        new("Company.CodeAlreadyExists",
            $"Company code '{code}' is already in use. Please choose a different code.");

    /// <summary>
    /// Company not found by ID.
    /// Maps to HTTP 404 Not Found in API layer.
    /// </summary>
    public static NotFoundError NotFound(Guid id) =>
        new("Company.NotFound",
            $"Company with ID '{id}' was not found.");

    /// <summary>
    /// Company not found by code.
    /// Maps to HTTP 404 Not Found in API layer.
    /// </summary>
    public static NotFoundError NotFoundByCode(string code) =>
        new("Company.NotFoundByCode",
            $"Company with code '{code}' was not found.");

    /// <summary>
    /// Company is inactive and cannot perform operations.
    /// Maps to HTTP 400 Bad Request in API layer.
    /// </summary>
    public static ValidationError CompanyInactive(string code) =>
        new("Company.Inactive",
            $"Company '{code}' is inactive. Please activate the company before performing this operation.");

    /// <summary>
    /// Company already active.
    /// Maps to HTTP 409 Conflict in API layer.
    /// </summary>
    public static ConflictError AlreadyActive(string code) =>
        new("Company.AlreadyActive",
            $"Company '{code}' is already active.");

    /// <summary>
    /// Company already inactive.
    /// Maps to HTTP 409 Conflict in API layer.
    /// </summary>
    public static ConflictError AlreadyInactive(string code) =>
        new("Company.AlreadyInactive",
            $"Company '{code}' is already inactive.");

    /// <summary>
    /// Cannot delete company with associated departments.
    /// Maps to HTTP 409 Conflict in API layer.
    /// </summary>
    public static ConflictError HasDepartments(string code) =>
        new("Company.HasDepartments",
            $"Cannot delete company '{code}' because it has associated departments. Please remove all departments first.");

    /// <summary>
    /// Cannot delete company with associated employees.
    /// Maps to HTTP 409 Conflict in API layer.
    /// </summary>
    public static ConflictError HasEmployees(string code) =>
        new("Company.HasEmployees",
            $"Cannot delete company '{code}' because it has associated employees. Please remove all employee assignments first.");
}
