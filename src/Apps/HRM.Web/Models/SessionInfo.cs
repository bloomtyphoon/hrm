namespace HRM.Web.Models;

/// <summary>
/// Active session information for session management UI.
/// </summary>
public sealed class SessionInfo
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? UserAgent { get; set; }
    public string? CreatedByIp { get; set; }
    public bool IsCurrent { get; set; }
}
