using HRM.Web.Models;

namespace HRM.Web.Services.Abstractions;

/// <summary>
/// API client for Organization module operations.
/// Handles company, department, and position management.
/// </summary>
public interface IOrganizationApiClient
{
    #region Companies

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

    #endregion

    #region Departments

    /// <summary>
    /// Get departments by company ID.
    /// </summary>
    Task<ApiResponse<IReadOnlyList<DepartmentResponse>>> GetDepartmentsByCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get department by ID.
    /// </summary>
    Task<ApiResponse<DepartmentResponse>> GetDepartmentByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    #endregion

    #region Positions

    /// <summary>
    /// Get positions by company ID.
    /// </summary>
    Task<ApiResponse<IReadOnlyList<PositionResponse>>> GetPositionsByCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get position by ID.
    /// </summary>
    Task<ApiResponse<PositionResponse>> GetPositionByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    #endregion
}
