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

/// <summary>
/// ViewModel for account detail page with roles.
/// </summary>
public sealed class AccountDetailViewModel
{
    /// <summary>
    /// Account information.
    /// </summary>
    public AccountSummary Account { get; set; } = new();

    /// <summary>
    /// Roles currently assigned to this account.
    /// </summary>
    public IReadOnlyList<RoleResponse> AssignedRoles { get; set; } = [];

    /// <summary>
    /// All available roles for assignment.
    /// </summary>
    public IReadOnlyList<RoleResponse> AvailableRoles { get; set; } = [];
}
