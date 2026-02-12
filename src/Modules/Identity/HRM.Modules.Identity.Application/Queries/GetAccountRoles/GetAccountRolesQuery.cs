using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Domain.Abstractions.Results;

namespace HRM.Modules.Identity.Application.Queries.GetAccountRoles;

/// <summary>
/// Query to get all roles assigned to an account.
/// </summary>
public sealed record GetAccountRolesQuery(Guid AccountId) : IQuery<Result<List<AccountRoleDto>>>;
