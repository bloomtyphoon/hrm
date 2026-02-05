namespace HRM.Web.Models;

/// <summary>
/// ViewModel for session management page.
/// </summary>
public sealed class SessionListViewModel
{
    public List<SessionInfo> Sessions { get; set; } = new();
}
