using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Organization.Application.Security;
using HRM.Modules.Organization.Domain.Errors;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Commands.ActivatePosition;

internal sealed class ActivatePositionCommandHandler : ICommandHandler<ActivatePositionCommand>
{
    private readonly IPositionRepository _positionRepository;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;

    public ActivatePositionCommandHandler(
        IPositionRepository positionRepository,
        IDataScopeService dataScopeService,
        IExecutionContext executionContext)
    {
        _positionRepository = positionRepository;
        _dataScopeService = dataScopeService;
        _executionContext = executionContext;
    }

    public async Task<Result> Handle(ActivatePositionCommand request, CancellationToken cancellationToken)
    {
        var position = await _positionRepository.GetByIdAsync(request.PositionId, cancellationToken);
        if (position is null)
        {
            return Result.Failure(PositionErrors.NotFound(request.PositionId));
        }

        var rule = await _dataScopeService.GetCompanyScopeRuleAsync(
            _executionContext.UserId, OrganizationPermissions.Position.Update, cancellationToken);

        if (!CanAccessCompany(position.CompanyId, rule))
        {
            return Result.Failure(new ForbiddenError(
                "Position.AccessDenied", "You do not have permission to activate this position."));
        }

        position.Activate();
        _positionRepository.Update(position);

        return Result.Success();
    }

    private static bool CanAccessCompany(Guid companyId, DataScopeRule rule) => rule.Level.Category switch
    {
        ScopeCategory.Global => true,
        ScopeCategory.Dimension => rule.DimensionIds.Contains(companyId),
        _ => false
    };
}
