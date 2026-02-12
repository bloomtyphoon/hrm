namespace HRM.Web.Models;

/// <summary>
/// Response model for account detail (from GET /api/identity/accounts/{id}).
/// </summary>
public sealed class AccountDetailResponse
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
}

/// <summary>
/// System profile response from API.
/// </summary>
public sealed class SystemProfileResponse
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public bool IsSuperAdmin { get; set; }
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
}

/// <summary>
/// Employee profile response from API.
/// </summary>
public sealed class EmployeeProfileResponse
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid EmployeeId { get; set; }
    public int DefaultScopeLevel { get; set; }
    public bool CanAccessAllAssignedCompanies { get; set; }
    public Guid? PrimaryCompanyId { get; set; }
    public Guid? PrimaryDepartmentId { get; set; }
    public Guid? PrimaryPositionId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
}

/// <summary>
/// Enable 2FA response.
/// </summary>
public sealed class EnableTwoFactorResponse
{
    public string SecretKey { get; set; } = string.Empty;
}

/// <summary>
/// Request model for a user changing their own password (requires current password).
/// </summary>
public sealed class ChangeMyPasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// Request model for admin resetting another account's password.
/// </summary>
public sealed class ResetAccountPasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// Update profile request model for Web UI.
/// </summary>
public sealed class UpdateProfileRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}

/// <summary>
/// Create system profile request.
/// </summary>
public sealed class CreateSystemProfileWebRequest
{
    public bool IsSuperAdmin { get; set; }
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
}

/// <summary>
/// Update system profile request.
/// </summary>
public sealed class UpdateSystemProfileWebRequest
{
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Create employee profile request.
/// </summary>
public sealed class CreateEmployeeProfileWebRequest
{
    public Guid EmployeeId { get; set; }
    public int DefaultScopeLevel { get; set; } = 4; // Self
    public Guid? PrimaryCompanyId { get; set; }
    public Guid? PrimaryDepartmentId { get; set; }
    public Guid? PrimaryPositionId { get; set; }
}

/// <summary>
/// Update employee profile request.
/// </summary>
public sealed class UpdateEmployeeProfileWebRequest
{
    public Guid? PrimaryCompanyId { get; set; }
    public Guid? PrimaryDepartmentId { get; set; }
    public Guid? PrimaryPositionId { get; set; }
    public int DefaultScopeLevel { get; set; }
    public bool CanAccessAllAssignedCompanies { get; set; }
}

/// <summary>
/// Scope level display helper.
/// </summary>
public static class ScopeLevelHelper
{
    public static string GetDisplayName(int level) => level switch
    {
        0 => "Global",
        1 => "Company",
        2 => "Department",
        3 => "Position",
        4 => "Self",
        _ => "Unknown"
    };

    public static string GetBadgeClass(int level) => level switch
    {
        0 => "bg-danger",
        1 => "bg-warning text-dark",
        2 => "bg-info",
        3 => "bg-primary",
        4 => "bg-secondary",
        _ => "bg-dark"
    };
}
