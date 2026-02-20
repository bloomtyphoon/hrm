using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.Modules.Organization.Application.DTOs;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Queries.GetPositionsByCompany;

internal sealed class GetPositionsByCompanyQueryHandler
    : IQueryHandler<GetPositionsByCompanyQuery, IReadOnlyList<PositionDto>>
{
    private readonly IPositionRepository _positionRepository;

    public GetPositionsByCompanyQueryHandler(IPositionRepository positionRepository)
    {
        _positionRepository = positionRepository;
    }

    public async Task<IReadOnlyList<PositionDto>> Handle(
        GetPositionsByCompanyQuery request,
        CancellationToken cancellationToken)
    {
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
}
