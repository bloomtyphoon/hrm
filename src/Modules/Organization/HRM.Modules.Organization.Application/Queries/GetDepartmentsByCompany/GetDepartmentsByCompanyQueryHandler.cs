using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Organization.Application.DTOs;
using HRM.Modules.Organization.Application.Security;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Queries.GetDepartmentsByCompany;

/// <summary>
/// Handler for GetDepartmentsByCompanyQuery.
///
/// Access rules (resolved via IDataScopeService):
/// - Global (System): can query departments of any company.
/// - Company scope: can query departments of assigned companies (multi-company support).
/// - None: empty result.
/// </summary>
internal sealed class GetDepartmentsByCompanyQueryHandler
    : IQueryHandler<GetDepartmentsByCompanyQuery, IReadOnlyList<DepartmentDto>>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;

    public GetDepartmentsByCompanyQueryHandler(
        IDepartmentRepository departmentRepository,
        IDataScopeService dataScopeService,
        IExecutionContext executionContext)
    {
        _departmentRepository = departmentRepository;
        _dataScopeService = dataScopeService;
        _executionContext = executionContext;
    }

    public async Task<IReadOnlyList<DepartmentDto>> Handle(
        GetDepartmentsByCompanyQuery request,
        CancellationToken cancellationToken)
    {
        var rule = await _dataScopeService.GetCompanyScopeRuleAsync(
            _executionContext.UserId, OrganizationPermissions.Company.View, cancellationToken);

        if (!CanAccessCompany(request.CompanyId, rule))
            return Array.Empty<DepartmentDto>();

        var departments = await _departmentRepository.GetByCompanyIdAsync(request.CompanyId, cancellationToken);

        return departments.Select(d => new DepartmentDto(
            Id: d.Id,
            Code: d.Code,
            Name: d.Name,
            CompanyId: d.CompanyId,
            ParentDepartmentId: d.ParentDepartmentId,
            ManagerId: d.ManagerId,
            Level: d.Level,
            Status: d.Status.ToString(),
            CreatedAtUtc: d.CreatedAtUtc,
            ModifiedAtUtc: d.ModifiedAtUtc
        )).ToList();
    }

    private static bool CanAccessCompany(Guid companyId, DataScopeRule rule) => rule.Level switch
    {
        DataScopeLevel.Global => true,
        DataScopeLevel.Company => rule.DimensionIds.Contains(companyId),
        _ => false
    };
}
