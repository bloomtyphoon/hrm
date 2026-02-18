using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.Modules.Organization.Application.DTOs;

namespace HRM.Modules.Organization.Application.Queries.GetPositionById;

public sealed record GetPositionByIdQuery(Guid PositionId) : IQuery<PositionDto?>;
