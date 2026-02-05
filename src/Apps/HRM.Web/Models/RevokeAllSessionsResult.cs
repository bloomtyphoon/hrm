namespace HRM.Web.Models;

/// <summary>
/// Result from revoking all sessions except current.
/// </summary>
public sealed class RevokeAllSessionsResult
{
    public int RevokedCount { get; set; }
    public string Message { get; set; } = string.Empty;
}
