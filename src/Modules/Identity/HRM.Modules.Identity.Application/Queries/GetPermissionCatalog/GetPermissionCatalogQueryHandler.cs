using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.Modules.Identity.Application.Abstractions.Authentication;
using HRM.Modules.Identity.Domain.Services;

namespace HRM.Modules.Identity.Application.Queries.GetPermissionCatalog;

public sealed class GetPermissionCatalogQueryHandler
    : IQueryHandler<GetPermissionCatalogQuery, PermissionCatalogDto>
{
    private readonly IPermissionCatalogService _catalogService;
    private readonly ICurrentUserService _currentUser;

    public GetPermissionCatalogQueryHandler(
        IPermissionCatalogService catalogService,
        ICurrentUserService currentUser)
    {
        _catalogService = catalogService;
        _currentUser = currentUser;
    }

    public async Task<PermissionCatalogDto> Handle(
        GetPermissionCatalogQuery request,
        CancellationToken cancellationToken)
    {
        // Load tenant-aware catalog when tenant context is available
        var modules = _currentUser.TenantId.HasValue
            ? await _catalogService.LoadCatalogAsync(_currentUser.TenantId.Value)
            : await _catalogService.LoadBaseCatalogAsync();

        var dto = new PermissionCatalogDto
        {
            Modules = modules.Select(m => new PermissionModuleDto
            {
                Name = m.Name,
                DisplayName = m.DisplayName,
                Entities = m.Entities.Select(e => new PermissionEntityDto
                {
                    Name = e.Name,
                    DisplayName = e.DisplayName,
                    Actions = e.Actions.Select(a => new PermissionActionDto
                    {
                        Name = a.Name,
                        DisplayName = a.DisplayName,
                        Scopes = a.Scopes.Select(s => new PermissionScopeDto
                        {
                            Value = s.Value,
                            DisplayName = s.DisplayName
                        }).ToList()
                    }).ToList()
                }).ToList()
            }).ToList()
        };

        return dto;
    }
}
