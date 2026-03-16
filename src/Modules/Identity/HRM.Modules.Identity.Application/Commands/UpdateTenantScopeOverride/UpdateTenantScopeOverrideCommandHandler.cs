using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Domain.Errors;
using HRM.Modules.Identity.Domain.Repositories;
using HRM.Modules.Identity.Domain.Services;

namespace HRM.Modules.Identity.Application.Commands.UpdateTenantScopeOverride;

internal sealed class UpdateTenantScopeOverrideCommandHandler
    : ICommandHandler<UpdateTenantScopeOverrideCommand>
{
    private readonly ITenantScopeOverrideRepository _repository;
    private readonly IPermissionCatalogService _catalogService;

    public UpdateTenantScopeOverrideCommandHandler(
        ITenantScopeOverrideRepository repository,
        IPermissionCatalogService catalogService)
    {
        _repository = repository;
        _catalogService = catalogService;
    }

    public async Task<Result> Handle(
        UpdateTenantScopeOverrideCommand request, CancellationToken cancellationToken)
    {
        // 1. Get existing override
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            return Result.Failure(TenantScopeOverrideErrors.NotFound(request.Id));
        }

        // 2. Validate requested scopes are subset of base catalog scopes
        var baseAction = await _catalogService.GetActionAsync(entity.Module, entity.Entity, entity.Action);
        if (baseAction is not null)
        {
            var permissionKey = entity.PermissionKey;
            foreach (var scope in request.AllowedScopes)
            {
                if (!baseAction.AllowsScope(scope))
                {
                    return Result.Failure(
                        TenantScopeOverrideErrors.ScopeNotInCatalog(permissionKey, scope.ToString()));
                }
            }
        }

        // 3. Update
        entity.Update(request.AllowedScopes, request.DefaultScope);

        return Result.Success();
    }
}
