using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Domain.Abstractions.Results;

namespace HRM.Modules.Identity.Application.Queries.GetRoleById;

/// <summary>
/// Query to retrieve a single role with its permissions.
/// </summary>
public sealed record GetRoleByIdQuery(Guid RoleId) : IQuery<Result<RoleDetailDto>>;
