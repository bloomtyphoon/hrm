namespace HRM.Web.Models;

/// <summary>
/// Response model from account API.
/// </summary>
public sealed class AccountResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public bool IsTwoFactorEnabled { get; set; }
    public DateTime? ActivatedAtUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }

    /// <summary>
    /// Roles assigned to this account.
    /// </summary>
    public List<AccountRoleInfo> Roles { get; set; } = [];
}

/// <summary>
/// Brief role information for account display.
/// </summary>
public sealed class AccountRoleInfo
{
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string RoleType { get; set; } = string.Empty;
}
