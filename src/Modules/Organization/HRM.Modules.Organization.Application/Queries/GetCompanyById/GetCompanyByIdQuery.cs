using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.Modules.Organization.Application.DTOs;

namespace HRM.Modules.Organization.Application.Queries.GetCompanyById;

public sealed record GetCompanyByIdQuery(Guid CompanyId) : IQuery<CompanyDto?>;
