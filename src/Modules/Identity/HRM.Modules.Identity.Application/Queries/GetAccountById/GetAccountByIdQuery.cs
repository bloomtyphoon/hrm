using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Domain.Abstractions.Results;

namespace HRM.Modules.Identity.Application.Queries.GetAccountById;

/// <summary>
/// Query to retrieve a single account by ID.
/// Returns full account detail with visibility check.
/// </summary>
public sealed record GetAccountByIdQuery(Guid AccountId) : IQuery<Result<AccountDetailDto>>;
