using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Application.Abstractions.Authentication;
using HRM.Modules.Identity.Domain.Entities;
using HRM.Modules.Identity.Domain.Errors;
using HRM.Modules.Identity.Domain.Repositories;
using HRM.Modules.Identity.Domain.Services;

namespace HRM.Modules.Identity.Application.Commands.CreateTenantScopeOverride;

internal sealed class CreateTenantScopeOverrideCommandHandler
    : ICommandHandler<CreateTenantScopeOverrideCommand, Guid>
{
    private readonly ITenantScopeOverrideRepository _repository;
    private readonly IPermissionCatalogService _catalogService;
    private readonly ICurrentUserService _currentUser;

    public CreateTenantScopeOverrideCommandHandler(
        ITenantScopeOverrideRepository repository,
        IPermissionCatalogService catalogService,
        ICurrentUserService currentUser)
    {
        _repository = repository;
        _catalogService = catalogService;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(
        CreateTenantScopeOverrideCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId
            ?? throw new InvalidOperationException("TenantId is required.");

        // 1. Validate permission exists in base catalog
        var baseAction = await _catalogService.GetActionAsync(request.Module, request.Entity, request.Action);
        if (baseAction is null)
        {
            return Result.Failure<Guid>(
                TenantScopeOverrideErrors.PermissionNotInCatalog(request.Module, request.Entity, request.Action));
        }

        // 2. Validate requested scopes are subset of base catalog scopes
        var permissionKey = $"{request.Module}.{request.Entity}.{request.Action}";
        foreach (var scope in request.AllowedScopes)
        {
            if (!baseAction.AllowsScope(scope))
            {
                return Result.Failure<Guid>(
                    TenantScopeOverrideErrors.ScopeNotInCatalog(permissionKey, scope.ToString()));
            }
        }

        // 3. Check uniqueness
        if (await _repository.ExistsAsync(request.Module, request.Entity, request.Action, cancellationToken))
        {
            return Result.Failure<Guid>(
                TenantScopeOverrideErrors.AlreadyExists(request.Module, request.Entity, request.Action));
        }

        // 4. Create entity
        var entity = TenantScopeOverride.Create(
            tenantId,
            request.Module,
            request.Entity,
            request.Action,
            request.AllowedScopes,
            request.DefaultScope);

        _repository.Add(entity);

        return Result.Success(entity.Id);
    }
}
