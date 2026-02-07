using HRM.Web.Models;

namespace HRM.Web.Services.Abstractions;

/// <summary>
/// API client for Organization module operations.
/// Handles company management.
/// </summary>
public interface IOrganizationApiClient
{
    /// <summary>
    /// Create a new company.
    /// </summary>
    Task<ApiResponse<CompanyResponse>> CreateCompanyAsync(
        CreateCompanyRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all companies.
    /// </summary>
    Task<ApiResponse<IReadOnlyList<CompanyResponse>>> GetCompaniesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get company by ID.
    /// </summary>
    Task<ApiResponse<CompanyResponse>> GetCompanyByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
