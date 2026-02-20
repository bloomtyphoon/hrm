using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.Modules.Organization.Application.DTOs;

namespace HRM.Modules.Organization.Application.Queries.GetPositionsByCompany;

public sealed record GetPositionsByCompanyQuery(Guid CompanyId) : IQuery<IReadOnlyList<PositionDto>>;
