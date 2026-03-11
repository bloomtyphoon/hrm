using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.Modules.Organization.Application.DTOs;

namespace HRM.Modules.Organization.Application.Queries.GetTenants;

public sealed record GetTenantsQuery : IQuery<IReadOnlyList<TenantDto>>;
