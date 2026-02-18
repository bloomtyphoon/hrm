using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Organization.Domain.Errors;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Commands.ClosePosition;

internal sealed class ClosePositionCommandHandler : ICommandHandler<ClosePositionCommand>
{
    private readonly IPositionRepository _positionRepository;

    public ClosePositionCommandHandler(IPositionRepository positionRepository)
    {
        _positionRepository = positionRepository;
    }

    public async Task<Result> Handle(ClosePositionCommand request, CancellationToken cancellationToken)
    {
        var position = await _positionRepository.GetByIdAsync(request.PositionId, cancellationToken);
        if (position is null)
        {
            return Result.Failure(PositionErrors.NotFound(request.PositionId));
        }

        position.Close();
        _positionRepository.Update(position);

        return Result.Success();
    }
}
