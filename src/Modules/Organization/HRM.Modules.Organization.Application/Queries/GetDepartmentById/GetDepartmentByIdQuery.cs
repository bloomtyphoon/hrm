using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.Modules.Organization.Application.DTOs;

namespace HRM.Modules.Organization.Application.Queries.GetDepartmentById;

public sealed record GetDepartmentByIdQuery(Guid DepartmentId) : IQuery<DepartmentDto?>;
