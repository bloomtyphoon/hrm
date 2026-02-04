using HRM.BuildingBlocks.Domain.Entities;
using HRM.Modules.Identity.Domain.Enums;

namespace HRM.Modules.Identity.Domain.Entities;

/// <summary>
/// Refresh Token entity for managing user sessions and token rotation.
///
/// Purpose:
/// - Store refresh tokens for JWT authentication
/// - Enable token revocation (logout, security breach)
/// - Support multi-device sessions
/// - Track session information (IP, user agent, device)
/// - Implement token rotation for security
///
/// Design:
/// - Uses AccountType + AccountId to identify the owning account
/// - Single table serves all account types (System, Employee)
/// - Enables unified session management across all account types
///
/// Token Lifecycle:
/// 1. Created: When user logs in
/// 2. Active: IsActive = true, not expired, not revoked
/// 3. Used: When refreshing access token (optionally rotated)
/// 4. Revoked: When user logs out or security event occurs
/// 5. Expired: ExpiresAt passes, automatically inactive
///
/// Database:
/// - Table: Identity.RefreshTokens
/// - Indexes: Token (unique), (AccountType, AccountId), ExpiresAt
/// - Soft delete: No (hard delete after expiration + grace period)
/// </summary>
public sealed class RefreshToken : AuditableEntity
{
    /// <summary>
    /// Type of account that owns this refresh token.
    /// Used as discriminator column in the database.
    ///
    /// Database: Stored as TINYINT (1 byte)
    /// </summary>
    public AccountType AccountType { get; private set; }

    /// <summary>
    /// ID of the account that owns this refresh token.
    /// References [Identity].Accounts.Id.
    /// </summary>
    public Guid AccountId { get; private set; }

    /// <summary>
    /// The refresh token value (random secure string).
    ///
    /// Properties:
    /// - 64 bytes (512 bits) of cryptographically secure random data
    /// - Base64-encoded, URL-safe
    /// - Must be unique across all tokens
    /// - Opaque (no user information embedded)
    /// </summary>
    public string Token { get; private set; } = string.Empty;

    /// <summary>
    /// Token expiration date/time (UTC).
    ///
    /// Expiration Strategy:
    /// - Normal login: 7 days (configurable)
    /// - Remember Me: 30 days (configurable)
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// When the token was revoked (UTC).
    /// NULL if token is still active.
    /// </summary>
    public DateTime? RevokedAt { get; private set; }

    /// <summary>
    /// IP address from which token was revoked.
    /// </summary>
    public string? RevokedByIp { get; private set; }

    /// <summary>
    /// New token that replaced this one (token rotation).
    /// NULL if not replaced (e.g., explicit logout).
    /// </summary>
    public string? ReplacedByToken { get; private set; }

    /// <summary>
    /// IP address from which token was created.
    /// </summary>
    public string CreatedByIp { get; private set; } = string.Empty;

    /// <summary>
    /// User agent (browser/device) that created the token.
    /// </summary>
    public string? UserAgent { get; private set; }

    /// <summary>
    /// Check if token is currently active and usable.
    /// Active = not revoked AND not expired.
    /// </summary>
    public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;

    /// <summary>
    /// Check if token is expired (regardless of revocation).
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    /// <summary>
    /// Private constructor for EF Core.
    /// </summary>
    private RefreshToken()
    {
    }

    /// <summary>
    /// Create new refresh token for an account.
    /// </summary>
    /// <param name="accountType">Type of account (System or Employee)</param>
    /// <param name="accountId">ID of the account that owns this token</param>
    /// <param name="token">Random secure token string</param>
    /// <param name="expiresAt">Expiration date/time (UTC)</param>
    /// <param name="ipAddress">IP address of the client</param>
    /// <param name="userAgent">User agent string (browser/device)</param>
    /// <returns>New RefreshToken instance</returns>
    public static RefreshToken Create(
        AccountType accountType,
        Guid accountId,
        string token,
        DateTime expiresAt,
        string? ipAddress,
        string? userAgent)
    {
        if (accountId == Guid.Empty)
        {
            throw new ArgumentException("Account ID cannot be empty", nameof(accountId));
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Token cannot be null or empty", nameof(token));
        }

        if (expiresAt <= DateTime.UtcNow)
        {
            throw new ArgumentException("Expiration date must be in the future", nameof(expiresAt));
        }

        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            AccountType = accountType,
            AccountId = accountId,
            Token = token,
            ExpiresAt = expiresAt,
            CreatedByIp = ipAddress ?? "unknown",
            UserAgent = userAgent
        };
    }

    /// <summary>
    /// Revoke this refresh token.
    /// Revocation is permanent - cannot be undone.
    /// </summary>
    /// <param name="ipAddress">IP address from which revocation was requested</param>
    /// <param name="replacedByToken">New token if this is token rotation (optional)</param>
    public void Revoke(string? ipAddress, string? replacedByToken = null)
    {
        if (RevokedAt.HasValue)
        {
            return;
        }

        RevokedAt = DateTime.UtcNow;
        RevokedByIp = ipAddress ?? "unknown";
        ReplacedByToken = replacedByToken;
    }
}
