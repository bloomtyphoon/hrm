using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.Modules.Organization.Application.DTOs;

namespace HRM.Modules.Organization.Application.Queries.GetCompanies;

public sealed record GetCompaniesQuery(bool? ActiveOnly = null) : IQuery<IReadOnlyList<CompanyDto>>;
