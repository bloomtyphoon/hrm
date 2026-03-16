using HRM.BuildingBlocks.Application.Abstractions.Queries;

namespace HRM.Modules.Identity.Application.Queries.GetTenantScopeOverrides;

/// <summary>
/// Query to retrieve all tenant scope overrides for the current tenant.
/// </summary>
public sealed record GetTenantScopeOverridesQuery : IQuery<List<TenantScopeOverrideDto>>;
