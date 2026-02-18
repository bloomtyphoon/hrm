using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Organization.Domain.Errors;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Commands.UpdatePosition;

internal sealed class UpdatePositionCommandHandler : ICommandHandler<UpdatePositionCommand>
{
    private readonly IPositionRepository _positionRepository;

    public UpdatePositionCommandHandler(IPositionRepository positionRepository)
    {
        _positionRepository = positionRepository;
    }

    public async Task<Result> Handle(UpdatePositionCommand request, CancellationToken cancellationToken)
    {
        var position = await _positionRepository.GetByIdAsync(request.PositionId, cancellationToken);
        if (position is null)
        {
            return Result.Failure(PositionErrors.NotFound(request.PositionId));
        }

        position.Update(
            title: request.Title,
            positionLevel: request.PositionLevel,
            isManagement: request.IsManagement,
            description: request.Description,
            maxHeadcount: request.MaxHeadcount
        );
        _positionRepository.Update(position);

        return Result.Success();
    }
}
