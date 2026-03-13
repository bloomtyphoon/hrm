using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Organization.Application.DTOs;
using HRM.Modules.Organization.Application.Security;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Queries.GetPositionsByCompany;

/// <summary>
/// Handler for GetPositionsByCompanyQuery.
///
/// Access rules (resolved via IDataScopeService):
/// - Global (System): can query positions of any company.
/// - Company scope: can query positions of assigned companies (multi-company support).
/// - None: empty result.
/// </summary>
internal sealed class GetPositionsByCompanyQueryHandler
    : IQueryHandler<GetPositionsByCompanyQuery, IReadOnlyList<PositionDto>>
{
    private readonly IPositionRepository _positionRepository;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;

    public GetPositionsByCompanyQueryHandler(
        IPositionRepository positionRepository,
        IDataScopeService dataScopeService,
        IExecutionContext executionContext)
    {
        _positionRepository = positionRepository;
        _dataScopeService = dataScopeService;
        _executionContext = executionContext;
    }

    public async Task<IReadOnlyList<PositionDto>> Handle(
        GetPositionsByCompanyQuery request,
        CancellationToken cancellationToken)
    {
        var rule = await _dataScopeService.GetCompanyScopeRuleAsync(
            _executionContext.UserId, OrganizationPermissions.Company.View, cancellationToken);

        if (!CanAccessCompany(request.CompanyId, rule))
            return Array.Empty<PositionDto>();

        var positions = await _positionRepository.GetByCompanyIdAsync(request.CompanyId, cancellationToken);

        return positions.Select(p => new PositionDto(
            Id: p.Id,
            Code: p.Code,
            Title: p.Title,
            Description: p.Description,
            CompanyId: p.CompanyId,
            DepartmentId: p.DepartmentId,
            PositionLevel: p.PositionLevel,
            IsManagement: p.IsManagement,
            MaxHeadcount: p.MaxHeadcount,
            Status: p.Status.ToString(),
            CreatedAtUtc: p.CreatedAtUtc,
            ModifiedAtUtc: p.ModifiedAtUtc
        )).ToList();
    }

    private static bool CanAccessCompany(Guid companyId, DataScopeRule rule) => rule.Level switch
    {
        DataScopeLevel.Global => true,
        DataScopeLevel.Company => rule.DimensionIds.Contains(companyId),
        _ => false
    };
}
