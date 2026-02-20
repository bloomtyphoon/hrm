using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.Modules.Organization.Application.DTOs;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Queries.GetPositionById;

internal sealed class GetPositionByIdQueryHandler : IQueryHandler<GetPositionByIdQuery, PositionDto?>
{
    private readonly IPositionRepository _positionRepository;

    public GetPositionByIdQueryHandler(IPositionRepository positionRepository)
    {
        _positionRepository = positionRepository;
    }

    public async Task<PositionDto?> Handle(GetPositionByIdQuery request, CancellationToken cancellationToken)
    {
        var position = await _positionRepository.GetByIdAsync(request.PositionId, cancellationToken);

        if (position is null)
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
}
