using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.Modules.Organization.Application.DTOs;

namespace HRM.Modules.Organization.Application.Queries.GetPositionsByDepartment;

public sealed record GetPositionsByDepartmentQuery(Guid DepartmentId) : IQuery<IReadOnlyList<PositionDto>>;
