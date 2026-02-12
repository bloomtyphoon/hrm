using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.Modules.Identity.Domain.Services;

namespace HRM.Modules.Identity.Application.Queries.GetPermissionCatalog;

public sealed class GetPermissionCatalogQueryHandler
    : IQueryHandler<GetPermissionCatalogQuery, PermissionCatalogDto>
{
    private readonly IPermissionCatalogService _catalogService;

    public GetPermissionCatalogQueryHandler(IPermissionCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public async Task<PermissionCatalogDto> Handle(
        GetPermissionCatalogQuery request,
        CancellationToken cancellationToken)
    {
        var modules = await _catalogService.LoadCatalogAsync();

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
