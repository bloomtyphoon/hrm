using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.Modules.Organization.Application.DTOs;

namespace HRM.Modules.Organization.Application.Queries.GetTenantById;

public sealed record GetTenantByIdQuery(Guid TenantId) : IQuery<TenantDto?>;
