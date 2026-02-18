using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Organization.Domain.Errors;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Commands.ActivatePosition;

internal sealed class ActivatePositionCommandHandler : ICommandHandler<ActivatePositionCommand>
{
    private readonly IPositionRepository _positionRepository;

    public ActivatePositionCommandHandler(IPositionRepository positionRepository)
    {
        _positionRepository = positionRepository;
    }

    public async Task<Result> Handle(ActivatePositionCommand request, CancellationToken cancellationToken)
    {
        var position = await _positionRepository.GetByIdAsync(request.PositionId, cancellationToken);
        if (position is null)
        {
            return Result.Failure(PositionErrors.NotFound(request.PositionId));
        }

        position.Activate();
        _positionRepository.Update(position);

        return Result.Success();
    }
}
