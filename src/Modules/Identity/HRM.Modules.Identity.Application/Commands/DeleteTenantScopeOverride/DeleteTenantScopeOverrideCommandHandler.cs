using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Domain.Errors;
using HRM.Modules.Identity.Domain.Repositories;

namespace HRM.Modules.Identity.Application.Commands.DeleteTenantScopeOverride;

internal sealed class DeleteTenantScopeOverrideCommandHandler
    : ICommandHandler<DeleteTenantScopeOverrideCommand>
{
    private readonly ITenantScopeOverrideRepository _repository;

    public DeleteTenantScopeOverrideCommandHandler(ITenantScopeOverrideRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(
        DeleteTenantScopeOverrideCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            return Result.Failure(TenantScopeOverrideErrors.NotFound(request.Id));
        }

        _repository.Remove(entity);

        return Result.Success();
    }
}
