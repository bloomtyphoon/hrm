using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Organization.Application.DTOs;
using HRM.Modules.Organization.Application.Security;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Queries.GetPositionById;

internal sealed class GetPositionByIdQueryHandler : IQueryHandler<GetPositionByIdQuery, PositionDto?>
{
    private readonly IPositionRepository _positionRepository;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;

    public GetPositionByIdQueryHandler(
        IPositionRepository positionRepository,
        IDataScopeService dataScopeService,
        IExecutionContext executionContext)
    {
        _positionRepository = positionRepository;
        _dataScopeService = dataScopeService;
        _executionContext = executionContext;
    }

    public async Task<PositionDto?> Handle(GetPositionByIdQuery request, CancellationToken cancellationToken)
    {
        var position = await _positionRepository.GetByIdAsync(request.PositionId, cancellationToken);

        if (position is null)
            return null;

        var rule = await _dataScopeService.GetCompanyScopeRuleAsync(
            _executionContext.UserId, OrganizationPermissions.Position.View, cancellationToken);

        if (!CanAccessCompany(position.CompanyId, rule))
            return null;

        return new PositionDto(
            Id: position.Id,
            Code: position.Code,
            Title: position.Title,
            Description: position.Description,
            CompanyId: position.CompanyId,
            DepartmentId: position.DepartmentId,
            PositionLevel: position.PositionLevel,
            IsManagement: position.IsManagement,
            MaxHeadcount: position.MaxHeadcount,
            Status: position.Status.ToString(),
            CreatedAtUtc: position.CreatedAtUtc,
            ModifiedAtUtc: position.ModifiedAtUtc
        );
    }

    private static bool CanAccessCompany(Guid companyId, DataScopeRule rule) => rule.Level.Category switch
    {
        ScopeCategory.Global => true,
        ScopeCategory.Dimension => rule.DimensionIds.Contains(companyId),
        _ => false
    };
}
