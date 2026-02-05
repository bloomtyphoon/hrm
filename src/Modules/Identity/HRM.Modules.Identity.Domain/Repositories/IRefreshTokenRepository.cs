using HRM.Modules.Identity.Domain.Entities;
using HRM.Modules.Identity.Domain.Enums;

namespace HRM.Modules.Identity.Domain.Repositories;

/// <summary>
/// Repository interface for RefreshToken entity.
/// Provides data access methods for session and token management.
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// Get refresh token by ID.
    /// </summary>
    Task<RefreshToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get refresh token by token string.
    /// </summary>
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get refresh token by token string and account (account type + ID).
    /// Validates token ownership before returning.
    /// </summary>
    Task<RefreshToken?> GetByTokenAndAccountAsync(
        string token,
        AccountType accountType,
        Guid accountId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active sessions for an account (except specified token).
    /// Active = not revoked AND not expired.
    /// </summary>
    Task<List<RefreshToken>> GetActiveSessionsExceptAsync(
        AccountType accountType,
        Guid accountId,
        Guid exceptTokenId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active sessions for an account.
    /// Ordered by CreatedAtUtc DESC (most recent first).
    /// </summary>
    Task<List<RefreshToken>> GetActiveSessionsAsync(
        AccountType accountType,
        Guid accountId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add new refresh token to repository.
    /// </summary>
    void Add(RefreshToken refreshToken);

    /// <summary>
    /// Update existing refresh token.
    /// </summary>
    void Update(RefreshToken refreshToken);

    /// <summary>
    /// Remove refresh token (hard delete).
    /// </summary>
    void Remove(RefreshToken refreshToken);
}
