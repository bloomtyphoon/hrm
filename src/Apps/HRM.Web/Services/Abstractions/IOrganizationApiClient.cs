using HRM.Web.Models;

namespace HRM.Web.Services.Abstractions;

/// <summary>
/// API client for Organization module operations.
/// Handles tenant, company, department, and position management.
/// </summary>
public interface IOrganizationApiClient
{
    #region Tenants

    Task<ApiResponse<TenantResponse>> CreateTenantAsync(
        CreateTenantRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<IReadOnlyList<TenantResponse>>> GetTenantsAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResponse<TenantResponse>> GetTenantByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<TenantResponse>> UpdateTenantAsync(
        Guid id,
        string name,
        string? subdomain,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<TenantResponse>> ActivateTenantAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<TenantResponse>> SuspendTenantAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<TenantResponse>> DeactivateTenantAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    #endregion

    #region Companies

    Task<ApiResponse<CompanyResponse>> CreateCompanyAsync(
        CreateCompanyRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<IReadOnlyList<CompanyResponse>>> GetCompaniesAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResponse<CompanyResponse>> GetCompanyByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<CompanyResponse>> UpdateCompanyAsync(
        Guid id,
        string name,
        string? taxId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<CompanyResponse>> ActivateCompanyAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<CompanyResponse>> DeactivateCompanyAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    #endregion

    #region Departments

    Task<ApiResponse<IReadOnlyList<DepartmentResponse>>> GetDepartmentsByCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<DepartmentResponse>> GetDepartmentByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<DepartmentResponse>> CreateDepartmentAsync(
        Guid companyId,
        string code,
        string name,
        Guid? parentDepartmentId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<DepartmentResponse>> UpdateDepartmentAsync(
        Guid id,
        string name,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<DepartmentResponse>> ActivateDepartmentAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<DepartmentResponse>> DeactivateDepartmentAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<object>> AssignDepartmentManagerAsync(
        Guid departmentId,
        Guid managerId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<object>> RemoveDepartmentManagerAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<object>> MoveDepartmentAsync(
        Guid departmentId,
        Guid? newParentDepartmentId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Positions

    Task<ApiResponse<IReadOnlyList<PositionResponse>>> GetPositionsByCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PositionResponse>> GetPositionByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PositionResponse>> CreatePositionAsync(
        Guid companyId,
        string code,
        string title,
        int positionLevel,
        bool isManagement,
        Guid? departmentId,
        string? description,
        int? maxHeadcount,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PositionResponse>> UpdatePositionAsync(
        Guid id,
        string title,
        int positionLevel,
        bool isManagement,
        string? description,
        int? maxHeadcount,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PositionResponse>> ActivatePositionAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PositionResponse>> DeactivatePositionAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PositionResponse>> ClosePositionAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<object>> MovePositionAsync(
        Guid positionId,
        Guid? departmentId,
        CancellationToken cancellationToken = default);

    #endregion
}
