using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.Modules.Organization.Application.DTOs;

namespace HRM.Modules.Organization.Application.Queries.GetDepartmentsByCompany;

public sealed record GetDepartmentsByCompanyQuery(Guid CompanyId) : IQuery<IReadOnlyList<DepartmentDto>>;
