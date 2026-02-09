using HRM.BuildingBlocks.Application.Abstractions.Queries;

namespace HRM.Modules.Identity.Application.Queries.GetPermissionCatalog;

/// <summary>
/// Query to retrieve the full permission catalog.
/// Used by admin UI for role permission selection.
/// </summary>
public sealed record GetPermissionCatalogQuery : IQuery<PermissionCatalogDto>;
