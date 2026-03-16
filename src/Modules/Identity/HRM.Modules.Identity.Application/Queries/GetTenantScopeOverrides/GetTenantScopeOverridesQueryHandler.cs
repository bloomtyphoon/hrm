using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.Modules.Identity.Domain.Repositories;

namespace HRM.Modules.Identity.Application.Queries.GetTenantScopeOverrides;

public sealed class GetTenantScopeOverridesQueryHandler
    : IQueryHandler<GetTenantScopeOverridesQuery, List<TenantScopeOverrideDto>>
{
    private readonly ITenantScopeOverrideRepository _repository;

    public GetTenantScopeOverridesQueryHandler(ITenantScopeOverrideRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<TenantScopeOverrideDto>> Handle(
        GetTenantScopeOverridesQuery request, CancellationToken cancellationToken)
    {
        var overrides = await _repository.GetAllAsync(cancellationToken);

        return overrides.Select(o => new TenantScopeOverrideDto
        {
            Id = o.Id,
            Module = o.Module,
            Entity = o.Entity,
            Action = o.Action,
            PermissionKey = o.PermissionKey,
            AllowedScopes = o.AllowedScopes,
            DefaultScope = o.DefaultScope,
            CreatedAtUtc = o.CreatedAtUtc,
            ModifiedAtUtc = o.ModifiedAtUtc
        }).ToList();
    }
}
