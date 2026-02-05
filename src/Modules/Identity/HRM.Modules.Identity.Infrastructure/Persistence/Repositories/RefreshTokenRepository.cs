using HRM.Modules.Identity.Domain.Enums;
using HRM.Modules.Identity.Domain.Entities;
using HRM.Modules.Identity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Identity.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for RefreshToken entity.
/// Uses AccountType + AccountId for discriminated queries.
/// </summary>
internal sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IdentityDbContext _context;

    public RefreshTokenRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<RefreshToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Id == id, cancellationToken);
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);
    }

    public async Task<RefreshToken?> GetByTokenAndAccountAsync(
        string token,
        AccountType accountType,
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(
                rt => rt.Token == token &&
                      rt.AccountType == accountType &&
                      rt.AccountId == accountId,
                cancellationToken
            );
    }

    public async Task<List<RefreshToken>> GetActiveSessionsExceptAsync(
        AccountType accountType,
        Guid accountId,
        Guid exceptTokenId,
        CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .Where(rt =>
                rt.AccountType == accountType &&
                rt.AccountId == accountId &&
                rt.Id != exceptTokenId &&
                rt.RevokedAt == null &&
                rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RefreshToken>> GetActiveSessionsAsync(
        AccountType accountType,
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .Where(rt =>
                rt.AccountType == accountType &&
                rt.AccountId == accountId &&
                rt.RevokedAt == null &&
                rt.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(rt => rt.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public void Add(RefreshToken refreshToken)
    {
        _context.RefreshTokens.Add(refreshToken);
    }

    public void Update(RefreshToken refreshToken)
    {
        _context.RefreshTokens.Update(refreshToken);
    }

    public void Remove(RefreshToken refreshToken)
    {
        _context.RefreshTokens.Remove(refreshToken);
    }
}
