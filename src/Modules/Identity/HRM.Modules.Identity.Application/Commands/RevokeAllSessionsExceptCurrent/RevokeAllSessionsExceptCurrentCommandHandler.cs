using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Domain.Enums;
using HRM.Modules.Identity.Application.Errors;
using HRM.Modules.Identity.Domain.Repositories;
using MediatR;

namespace HRM.Modules.Identity.Application.Commands.RevokeAllSessionsExceptCurrent;

/// <summary>
/// Handler for RevokeAllSessionsExceptCurrentCommand.
/// Revokes all account's sessions except current device.
///
/// Business Logic:
/// 1. Verify current token exists and belongs to account
/// 2. Find all active sessions for account
/// 3. Filter out current session
/// 4. Revoke all others in bulk
/// 5. Return count of revoked sessions
/// </summary>
public sealed class RevokeAllSessionsExceptCurrentCommandHandler
    : IRequestHandler<RevokeAllSessionsExceptCurrentCommand, Result<RevokeAllSessionsResult>>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public RevokeAllSessionsExceptCurrentCommandHandler(IRefreshTokenRepository refreshTokenRepository)
    {
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<Result<RevokeAllSessionsResult>> Handle(
        RevokeAllSessionsExceptCurrentCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Verify current token exists and belongs to account
        var currentToken = await _refreshTokenRepository.GetByTokenAndAccountAsync(
            request.CurrentRefreshToken,
            AccountType.System,
            request.AccountId,
            cancellationToken);

        if (currentToken is null)
        {
            return Result.Failure<RevokeAllSessionsResult>(
                SessionErrors.CannotIdentifyCurrentSession());
        }

        // 2. Find all active sessions for account (except current)
        var sessionsToRevoke = await _refreshTokenRepository.GetActiveSessionsExceptAsync(
            AccountType.System,
            request.AccountId,
            currentToken.Id,
            cancellationToken);

        // 3. If no sessions to revoke, return success
        if (sessionsToRevoke.Count == 0)
        {
            return Result.Success(new RevokeAllSessionsResult
            {
                RevokedCount = 0,
                Message = "No other active sessions found"
            });
        }

        // 4. Revoke all sessions
        foreach (var session in sessionsToRevoke)
        {
            session.Revoke(request.IpAddress);
        }

        // UnitOfWorkBehavior will commit

        // 5. Return result
        var message = sessionsToRevoke.Count == 1
            ? "1 session was terminated"
            : $"{sessionsToRevoke.Count} sessions were terminated";

        return Result.Success(new RevokeAllSessionsResult
        {
            RevokedCount = sessionsToRevoke.Count,
            Message = message
        });
    }
}
