using HRM.Web.Models;
using HRM.Web.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Web.Controllers;

/// <summary>
/// Controller for account management, profiles, and session management.
/// </summary>
[Authorize]
public class AccountController : Controller
{
    private readonly IIdentityApiClient _identityClient;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IIdentityApiClient identityClient,
        ILogger<AccountController> logger)
    {
        _identityClient = identityClient;
        _logger = logger;
    }

    #region Account List & Detail

    [HttpGet]
    public async Task<IActionResult> Index(
        string? searchTerm = null,
        string? status = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var response = await _identityClient.GetAccountsAsync(
            searchTerm, status, pageNumber, pageSize, cancellationToken);

        var viewModel = new AccountListViewModel
        {
            SearchTerm = searchTerm,
            StatusFilter = status,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        if (response.IsSuccess && response.Data != null)
        {
            viewModel.Accounts = response.Data;
        }
        else
        {
            _logger.LogError("Failed to get accounts: {ErrorMessage}", response.ErrorMessage);
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to load accounts";
            viewModel.Accounts = new PagedResult<AccountSummary>();
        }

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var accountResponse = await _identityClient.GetAccountByIdAsync(id, cancellationToken);

        if (!accountResponse.IsSuccess || accountResponse.Data == null)
        {
            TempData["ErrorMessage"] = accountResponse.ErrorMessage ?? "Account not found.";
            return RedirectToAction(nameof(Index));
        }

        var account = accountResponse.Data;

        // Fetch roles
        var accountRolesResponse = await _identityClient.GetAccountRolesAsync(id, cancellationToken);
        var assignedRoles = accountRolesResponse.IsSuccess && accountRolesResponse.Data != null
            ? accountRolesResponse.Data : [];

        var allRolesResponse = await _identityClient.GetRolesAsync(cancellationToken);
        var availableRoles = allRolesResponse.IsSuccess && allRolesResponse.Data != null
            ? allRolesResponse.Data.Where(r => r.IsActive).ToList() : [];

        var viewModel = new AccountDetailViewModel
        {
            Account = account,
            AssignedRoles = assignedRoles,
            AvailableRoles = availableRoles
        };

        // Fetch profile based on account type
        if (account.AccountType == "System")
        {
            var profileResponse = await _identityClient.GetSystemProfileAsync(id, cancellationToken);
            if (profileResponse.IsSuccess)
                viewModel.SystemProfile = profileResponse.Data;
        }
        else if (account.AccountType == "Employee")
        {
            var profileResponse = await _identityClient.GetEmployeeProfileAsync(id, cancellationToken);
            if (profileResponse.IsSuccess)
                viewModel.EmployeeProfile = profileResponse.Data;
        }

        return View(viewModel);
    }

    #endregion

    #region Account Actions

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _identityClient.ActivateAccountAsync(id, cancellationToken);

        if (response.IsSuccess)
            TempData["SuccessMessage"] = "Account has been activated.";
        else
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to activate account.";

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Suspend(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _identityClient.SuspendAccountAsync(id, cancellationToken);

        if (response.IsSuccess)
            TempData["SuccessMessage"] = "Account has been suspended.";
        else
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to suspend account.";

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _identityClient.DeactivateAccountAsync(id, cancellationToken);

        if (response.IsSuccess)
            TempData["SuccessMessage"] = "Account has been deactivated.";
        else
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to deactivate account.";

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _identityClient.UnlockAccountAsync(id, cancellationToken);

        if (response.IsSuccess)
            TempData["SuccessMessage"] = "Account has been unlocked.";
        else
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to unlock account.";

        return RedirectToAction(nameof(Detail), new { id });
    }

    #endregion

    #region Two-Factor Authentication

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EnableTwoFactor(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _identityClient.EnableTwoFactorAsync(id, cancellationToken);

        if (response.IsSuccess && response.Data != null)
        {
            TempData["SuccessMessage"] = "Two-factor authentication has been enabled.";
            TempData["TwoFactorSecretKey"] = response.Data.SecretKey;
        }
        else
        {
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to enable 2FA.";
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DisableTwoFactor(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _identityClient.DisableTwoFactorAsync(id, cancellationToken);

        if (response.IsSuccess)
            TempData["SuccessMessage"] = "Two-factor authentication has been disabled.";
        else
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to disable 2FA.";

        return RedirectToAction(nameof(Detail), new { id });
    }

    #endregion

    #region Password & Profile

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(
        Guid id, string newPassword, CancellationToken cancellationToken = default)
    {
        var request = new ChangePasswordRequest
        {
            NewPassword = newPassword,
            IsAdminReset = true
        };

        var response = await _identityClient.ChangePasswordAsync(id, request, cancellationToken);

        if (response.IsSuccess)
            TempData["SuccessMessage"] = "Password has been reset.";
        else
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to change password.";

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(
        Guid id, string fullName, string? phoneNumber, CancellationToken cancellationToken = default)
    {
        var request = new UpdateProfileRequest
        {
            FullName = fullName,
            PhoneNumber = phoneNumber
        };

        var response = await _identityClient.UpdateProfileAsync(id, request, cancellationToken);

        if (response.IsSuccess)
            TempData["SuccessMessage"] = "Profile has been updated.";
        else
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to update profile.";

        return RedirectToAction(nameof(Detail), new { id });
    }

    #endregion

    #region Role Management

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRoles(
        Guid id, List<Guid> roleIds, CancellationToken cancellationToken = default)
    {
        var request = new AssignRolesToAccountRequest { RoleIds = roleIds ?? [] };
        var response = await _identityClient.AssignRolesToAccountAsync(id, request, cancellationToken);

        if (response.IsSuccess)
            TempData["SuccessMessage"] = "Roles updated successfully.";
        else
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to update roles.";

        return RedirectToAction(nameof(Detail), new { id });
    }

    #endregion

    #region System Profile

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSystemProfile(
        Guid id, bool isSuperAdmin, string? department, string? jobTitle,
        CancellationToken cancellationToken = default)
    {
        var request = new CreateSystemProfileWebRequest
        {
            IsSuperAdmin = isSuperAdmin,
            Department = department,
            JobTitle = jobTitle
        };

        var response = await _identityClient.CreateSystemProfileAsync(id, request, cancellationToken);

        if (response.IsSuccess)
            TempData["SuccessMessage"] = "System profile created.";
        else
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to create system profile.";

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSystemProfile(
        Guid id, string? department, string? jobTitle, string? notes,
        CancellationToken cancellationToken = default)
    {
        var request = new UpdateSystemProfileWebRequest
        {
            Department = department,
            JobTitle = jobTitle,
            Notes = notes
        };

        var response = await _identityClient.UpdateSystemProfileAsync(id, request, cancellationToken);

        if (response.IsSuccess)
            TempData["SuccessMessage"] = "System profile updated.";
        else
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to update system profile.";

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GrantSuperAdmin(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _identityClient.GrantSuperAdminAsync(id, cancellationToken);

        if (response.IsSuccess)
            TempData["SuccessMessage"] = "Super admin privileges granted.";
        else
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to grant super admin.";

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeSuperAdmin(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _identityClient.RevokeSuperAdminAsync(id, cancellationToken);

        if (response.IsSuccess)
            TempData["SuccessMessage"] = "Super admin privileges revoked.";
        else
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to revoke super admin.";

        return RedirectToAction(nameof(Detail), new { id });
    }

    #endregion

    #region Employee Profile

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEmployeeProfile(
        Guid id, Guid employeeId, int defaultScopeLevel,
        Guid? primaryCompanyId, Guid? primaryDepartmentId, Guid? primaryPositionId,
        CancellationToken cancellationToken = default)
    {
        var request = new CreateEmployeeProfileWebRequest
        {
            EmployeeId = employeeId,
            DefaultScopeLevel = defaultScopeLevel,
            PrimaryCompanyId = primaryCompanyId,
            PrimaryDepartmentId = primaryDepartmentId,
            PrimaryPositionId = primaryPositionId
        };

        var response = await _identityClient.CreateEmployeeProfileAsync(id, request, cancellationToken);

        if (response.IsSuccess)
            TempData["SuccessMessage"] = "Employee profile created.";
        else
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to create employee profile.";

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateEmployeeProfile(
        Guid id, int defaultScopeLevel, bool canAccessAllAssignedCompanies,
        Guid? primaryCompanyId, Guid? primaryDepartmentId, Guid? primaryPositionId,
        CancellationToken cancellationToken = default)
    {
        var request = new UpdateEmployeeProfileWebRequest
        {
            DefaultScopeLevel = defaultScopeLevel,
            CanAccessAllAssignedCompanies = canAccessAllAssignedCompanies,
            PrimaryCompanyId = primaryCompanyId,
            PrimaryDepartmentId = primaryDepartmentId,
            PrimaryPositionId = primaryPositionId
        };

        var response = await _identityClient.UpdateEmployeeProfileAsync(id, request, cancellationToken);

        if (response.IsSuccess)
            TempData["SuccessMessage"] = "Employee profile updated.";
        else
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to update employee profile.";

        return RedirectToAction(nameof(Detail), new { id });
    }

    #endregion

    #region Registration

    [HttpGet]
    public IActionResult Register() => View(new RegisterAccountRequest());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        RegisterAccountRequest model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);

        var response = await _identityClient.RegisterAccountAsync(model, cancellationToken);

        if (response.IsSuccess && response.Data != null)
        {
            TempData["SuccessMessage"] = $"Account '{response.Data.Username}' registered successfully!";
            return RedirectToAction(nameof(Detail), new { id = response.Data.Id });
        }

        if (response.ValidationErrors != null)
        {
            foreach (var (field, errors) in response.ValidationErrors)
                foreach (var error in errors)
                    ModelState.AddModelError(field, error);
        }
        else
        {
            ModelState.AddModelError(string.Empty, response.ErrorMessage ?? "Registration failed");
        }

        return View(model);
    }

    #endregion

    #region Sessions

    [HttpGet]
    public async Task<IActionResult> Sessions(CancellationToken cancellationToken = default)
    {
        var response = await _identityClient.GetActiveSessionsAsync(cancellationToken);
        var viewModel = new SessionListViewModel();

        if (response.IsSuccess && response.Data != null)
            viewModel.Sessions = response.Data;
        else
        {
            _logger.LogError("Failed to get sessions: {ErrorMessage}", response.ErrorMessage);
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to load sessions.";
        }

        return View(viewModel);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeSession(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var response = await _identityClient.RevokeSessionAsync(sessionId, cancellationToken);

        if (response.IsSuccess)
            TempData["SuccessMessage"] = "Session has been revoked.";
        else
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to revoke session.";

        return RedirectToAction(nameof(Sessions));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeAllSessions(CancellationToken cancellationToken = default)
    {
        var response = await _identityClient.RevokeAllSessionsExceptCurrentAsync(cancellationToken);

        if (response.IsSuccess && response.Data != null)
            TempData["SuccessMessage"] = response.Data.Message;
        else
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to revoke sessions.";

        return RedirectToAction(nameof(Sessions));
    }

    #endregion
}
