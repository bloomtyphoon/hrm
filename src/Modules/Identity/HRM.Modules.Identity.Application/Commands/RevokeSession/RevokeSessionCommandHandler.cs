using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Application.Errors;
using HRM.Modules.Identity.Domain.Repositories;
using MediatR;

namespace HRM.Modules.Identity.Application.Commands.RevokeSession;

/// <summary>
/// Handler for RevokeSessionCommand.
/// Revokes specific session (logout from specific device).
/// </summary>
public sealed class RevokeSessionCommandHandler
    : IRequestHandler<RevokeSessionCommand, Result>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public RevokeSessionCommandHandler(IRefreshTokenRepository refreshTokenRepository)
    {
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<Result> Handle(
        RevokeSessionCommand request,
        CancellationToken cancellationToken)
    {
        var session = await _refreshTokenRepository.GetByIdAsync(
            request.SessionId,
            cancellationToken);

        if (session is null)
        {
            return Result.Failure(SessionErrors.NotFound());
        }

        // Security: Verify session belongs to current account
        if (session.AccountId != request.AccountId)
        {
            return Result.Failure(SessionErrors.UnauthorizedAccess());
        }

        if (session.RevokedAt.HasValue)
        {
            return Result.Success();
        }

        session.Revoke(request.IpAddress);

        return Result.Success();
    }
}
