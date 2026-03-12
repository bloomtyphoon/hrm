using System.Net.Http.Json;
using HRM.Web.Models;
using HRM.Web.Services.Abstractions;

namespace HRM.Web.Services.Organization;

/// <summary>
/// HTTP client for Organization module API endpoints.
/// </summary>
public sealed class OrganizationApiClient(
    HttpClient httpClient,
    ILogger<OrganizationApiClient> logger)
    : ApiClientBase(httpClient, logger), IOrganizationApiClient
{
    #region Tenants

    public Task<ApiResponse<TenantResponse>> CreateTenantAsync(
        CreateTenantRequest request,
        CancellationToken cancellationToken = default)
        => PostAsync<TenantResponse>("/api/organization/tenants",
            new { request.Code, request.Name, request.Subdomain }, "Failed to create tenant", cancellationToken);

    public async Task<ApiResponse<IReadOnlyList<TenantResponse>>> GetTenantsAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await HttpClient.GetAsync("/api/organization/tenants", cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<List<TenantResponse>>(JsonOptions, cancellationToken);
            return new ApiResponse<IReadOnlyList<TenantResponse>> { IsSuccess = true, Data = data ?? [] };
        }
        return await HandleErrorResponseAsync<IReadOnlyList<TenantResponse>>(response, "Failed to retrieve tenants", cancellationToken);
    }

    public Task<ApiResponse<TenantResponse>> GetTenantByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
        => GetAsync<TenantResponse>($"/api/organization/tenants/{id}", "Failed to retrieve tenant", cancellationToken);

    public Task<ApiResponse<TenantResponse>> UpdateTenantAsync(
        Guid id, string name, string? subdomain, CancellationToken cancellationToken = default)
        => PutAsync<TenantResponse>($"/api/organization/tenants/{id}",
            new { Name = name, Subdomain = subdomain }, "Failed to update tenant", cancellationToken);

    public Task<ApiResponse<TenantResponse>> ActivateTenantAsync(
        Guid id, CancellationToken cancellationToken = default)
        => PutAsync<TenantResponse>($"/api/organization/tenants/{id}/activate",
            new { }, "Failed to activate tenant", cancellationToken);

    public Task<ApiResponse<TenantResponse>> SuspendTenantAsync(
        Guid id, CancellationToken cancellationToken = default)
        => PutAsync<TenantResponse>($"/api/organization/tenants/{id}/suspend",
            new { }, "Failed to suspend tenant", cancellationToken);

    public Task<ApiResponse<TenantResponse>> DeactivateTenantAsync(
        Guid id, CancellationToken cancellationToken = default)
        => PutAsync<TenantResponse>($"/api/organization/tenants/{id}/deactivate",
            new { }, "Failed to deactivate tenant", cancellationToken);

    #endregion

    #region Companies

    public async Task<ApiResponse<CompanyResponse>> CreateCompanyAsync(
        CreateCompanyRequest request,
        CancellationToken cancellationToken = default)
        => await PostAsync<CompanyResponse>("/api/organization/companies",
            new { request.Code, request.Name, request.TaxId }, "Failed to create company", cancellationToken);

    public async Task<ApiResponse<IReadOnlyList<CompanyResponse>>> GetCompaniesAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await HttpClient.GetAsync("/api/organization/companies", cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<List<CompanyResponse>>(JsonOptions, cancellationToken);
            return new ApiResponse<IReadOnlyList<CompanyResponse>> { IsSuccess = true, Data = data ?? [] };
        }
        return await HandleErrorResponseAsync<IReadOnlyList<CompanyResponse>>(response, "Failed to retrieve companies", cancellationToken);
    }

    public Task<ApiResponse<CompanyResponse>> GetCompanyByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
        => GetAsync<CompanyResponse>($"/api/organization/companies/{id}", "Failed to retrieve company", cancellationToken);

    public Task<ApiResponse<CompanyResponse>> UpdateCompanyAsync(
        Guid id, string name, string? taxId, CancellationToken cancellationToken = default)
        => PutAsync<CompanyResponse>($"/api/organization/companies/{id}",
            new { Name = name, TaxId = taxId }, "Failed to update company", cancellationToken);

    public Task<ApiResponse<CompanyResponse>> ActivateCompanyAsync(
        Guid id, CancellationToken cancellationToken = default)
        => PutAsync<CompanyResponse>($"/api/organization/companies/{id}/activate",
            new { }, "Failed to activate company", cancellationToken);

    public Task<ApiResponse<CompanyResponse>> DeactivateCompanyAsync(
        Guid id, CancellationToken cancellationToken = default)
        => PutAsync<CompanyResponse>($"/api/organization/companies/{id}/deactivate",
            new { }, "Failed to deactivate company", cancellationToken);

    #endregion

    #region Departments

    public async Task<ApiResponse<IReadOnlyList<DepartmentResponse>>> GetDepartmentsByCompanyAsync(
        Guid companyId, CancellationToken cancellationToken = default)
    {
        var response = await HttpClient.GetAsync(
            $"/api/organization/departments?companyId={companyId}", cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<List<DepartmentResponse>>(JsonOptions, cancellationToken);
            return new ApiResponse<IReadOnlyList<DepartmentResponse>> { IsSuccess = true, Data = data ?? [] };
        }
        return await HandleErrorResponseAsync<IReadOnlyList<DepartmentResponse>>(response, "Failed to retrieve departments", cancellationToken);
    }

    public Task<ApiResponse<DepartmentResponse>> GetDepartmentByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
        => GetAsync<DepartmentResponse>($"/api/organization/departments/{id}", "Failed to retrieve department", cancellationToken);

    public Task<ApiResponse<DepartmentResponse>> CreateDepartmentAsync(
        Guid companyId, string code, string name, Guid? parentDepartmentId,
        CancellationToken cancellationToken = default)
        => PostAsync<DepartmentResponse>("/api/organization/departments",
            new { CompanyId = companyId, Code = code, Name = name, ParentDepartmentId = parentDepartmentId },
            "Failed to create department", cancellationToken);

    public Task<ApiResponse<DepartmentResponse>> UpdateDepartmentAsync(
        Guid id, string name, CancellationToken cancellationToken = default)
        => PutAsync<DepartmentResponse>($"/api/organization/departments/{id}",
            new { Name = name }, "Failed to update department", cancellationToken);

    public Task<ApiResponse<DepartmentResponse>> ActivateDepartmentAsync(
        Guid id, CancellationToken cancellationToken = default)
        => PutAsync<DepartmentResponse>($"/api/organization/departments/{id}/activate",
            new { }, "Failed to activate department", cancellationToken);

    public Task<ApiResponse<DepartmentResponse>> DeactivateDepartmentAsync(
        Guid id, CancellationToken cancellationToken = default)
        => PutAsync<DepartmentResponse>($"/api/organization/departments/{id}/deactivate",
            new { }, "Failed to deactivate department", cancellationToken);

    public Task<ApiResponse<object>> AssignDepartmentManagerAsync(
        Guid departmentId, Guid managerId, CancellationToken cancellationToken = default)
        => PutAsync<object>($"/api/organization/departments/{departmentId}/manager",
            new { ManagerId = managerId }, "Failed to assign department manager", cancellationToken);

    public Task<ApiResponse<object>> RemoveDepartmentManagerAsync(
        Guid departmentId, CancellationToken cancellationToken = default)
        => DeleteAsync<object>($"/api/organization/departments/{departmentId}/manager",
            "Failed to remove department manager", cancellationToken);

    public Task<ApiResponse<object>> MoveDepartmentAsync(
        Guid departmentId, Guid? newParentDepartmentId, CancellationToken cancellationToken = default)
        => PutAsync<object>($"/api/organization/departments/{departmentId}/move",
            new { NewParentDepartmentId = newParentDepartmentId }, "Failed to move department", cancellationToken);

    #endregion

    #region Positions

    public async Task<ApiResponse<IReadOnlyList<PositionResponse>>> GetPositionsByCompanyAsync(
        Guid companyId, CancellationToken cancellationToken = default)
    {
        var response = await HttpClient.GetAsync(
            $"/api/organization/positions?companyId={companyId}", cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<List<PositionResponse>>(JsonOptions, cancellationToken);
            return new ApiResponse<IReadOnlyList<PositionResponse>> { IsSuccess = true, Data = data ?? [] };
        }
        return await HandleErrorResponseAsync<IReadOnlyList<PositionResponse>>(response, "Failed to retrieve positions", cancellationToken);
    }

    public async Task<ApiResponse<IReadOnlyList<PositionResponse>>> GetPositionsByDepartmentAsync(
        Guid departmentId, CancellationToken cancellationToken = default)
    {
        var response = await HttpClient.GetAsync(
            $"/api/organization/positions/by-department/{departmentId}", cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<List<PositionResponse>>(JsonOptions, cancellationToken);
            return new ApiResponse<IReadOnlyList<PositionResponse>> { IsSuccess = true, Data = data ?? [] };
        }
        return await HandleErrorResponseAsync<IReadOnlyList<PositionResponse>>(response, "Failed to retrieve positions by department", cancellationToken);
    }

    public Task<ApiResponse<PositionResponse>> GetPositionByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
        => GetAsync<PositionResponse>($"/api/organization/positions/{id}", "Failed to retrieve position", cancellationToken);

    public Task<ApiResponse<PositionResponse>> CreatePositionAsync(
        Guid companyId, string code, string title, int positionLevel, bool isManagement,
        Guid? departmentId, string? description, int? maxHeadcount,
        CancellationToken cancellationToken = default)
        => PostAsync<PositionResponse>("/api/organization/positions",
            new
            {
                CompanyId = companyId, Code = code, Title = title, PositionLevel = positionLevel,
                IsManagement = isManagement, DepartmentId = departmentId,
                Description = description, MaxHeadcount = maxHeadcount
            },
            "Failed to create position", cancellationToken);

    public Task<ApiResponse<PositionResponse>> UpdatePositionAsync(
        Guid id, string title, int positionLevel, bool isManagement,
        string? description, int? maxHeadcount,
        CancellationToken cancellationToken = default)
        => PutAsync<PositionResponse>($"/api/organization/positions/{id}",
            new
            {
                Title = title, PositionLevel = positionLevel, IsManagement = isManagement,
                Description = description, MaxHeadcount = maxHeadcount
            },
            "Failed to update position", cancellationToken);

    public Task<ApiResponse<PositionResponse>> ActivatePositionAsync(
        Guid id, CancellationToken cancellationToken = default)
        => PutAsync<PositionResponse>($"/api/organization/positions/{id}/activate",
            new { }, "Failed to activate position", cancellationToken);

    public Task<ApiResponse<PositionResponse>> DeactivatePositionAsync(
        Guid id, CancellationToken cancellationToken = default)
        => PutAsync<PositionResponse>($"/api/organization/positions/{id}/deactivate",
            new { }, "Failed to deactivate position", cancellationToken);

    public Task<ApiResponse<PositionResponse>> ClosePositionAsync(
        Guid id, CancellationToken cancellationToken = default)
        => PutAsync<PositionResponse>($"/api/organization/positions/{id}/close",
            new { }, "Failed to close position", cancellationToken);

    public Task<ApiResponse<object>> MovePositionAsync(
        Guid positionId, Guid? departmentId, CancellationToken cancellationToken = default)
        => PutAsync<object>($"/api/organization/positions/{positionId}/move",
            new { DepartmentId = departmentId }, "Failed to move position", cancellationToken);

    #endregion
}
