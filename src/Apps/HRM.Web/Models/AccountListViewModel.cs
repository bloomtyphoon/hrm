namespace HRM.Web.Models;

/// <summary>
/// ViewModel for account list page.
/// </summary>
public sealed class AccountListViewModel
{
    public PagedResult<AccountSummary> Accounts { get; set; } = new();
    public string? SearchTerm { get; set; }
    public string? StatusFilter { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public static readonly List<string> StatusOptions = new()
    {
        "Pending",
        "Active",
        "Suspended",
        "Deactivated"
    };
}
