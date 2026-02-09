using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Application.Queries.GetAccounts;

namespace HRM.Modules.Identity.Application.Queries.GetAccountById;

/// <summary>
/// Query to retrieve a single account by ID.
/// </summary>
public sealed record GetAccountByIdQuery(Guid AccountId) : IQuery<Result<AccountSummaryDto>>;
