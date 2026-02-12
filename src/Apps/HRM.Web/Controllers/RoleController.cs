using HRM.Web.Models;
using HRM.Web.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Web.Controllers;

/// <summary>
/// Controller for role management.
/// Handles role CRUD and permission assignment.
/// </summary>
[Authorize]
public class RoleController : Controller
{
    private readonly IIdentityApiClient _identityClient;
    private readonly ICompanyContext _companyContext;
    private readonly ILogger<RoleController> _logger;

    public RoleController(
        IIdentityApiClient identityClient,
        ICompanyContext companyContext,
        ILogger<RoleController> logger)
    {
        _identityClient = identityClient;
        _companyContext = companyContext;
        _logger = logger;
    }

    /// <summary>
    /// GET: /Role or /Role/Index
    /// Display list of roles.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(
        string? searchTerm = null,
        string? roleType = null,
        CancellationToken cancellationToken = default)
    {
        var response = await _identityClient.GetRolesAsync(
            _companyContext.SelectedCompanyId,
            _companyContext.IsAllCompanies,
            cancellationToken);

        var viewModel = new RoleListViewModel
        {
            SearchTerm = searchTerm,
            RoleTypeFilter = roleType
        };

        if (response.IsSuccess && response.Data != null)
        {
            var roles = response.Data;

            // Apply client-side filtering
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                roles = roles
                    .Where(r =>
                        r.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        (r.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(roleType))
            {
                roles = roles
                    .Where(r => r.RoleType.Equals(roleType, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            viewModel.Roles = roles;
        }
        else
        {
            _logger.LogError("Failed to get roles: {ErrorMessage}", response.ErrorMessage);
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to load roles";
            viewModel.Roles = [];
        }

        return View(viewModel);
    }

    /// <summary>
    /// GET: /Role/Details/{id}
    /// Display role details with permissions.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var response = await _identityClient.GetRoleByIdAsync(id, cancellationToken);

        if (response.IsSuccess && response.Data != null)
        {
            var viewModel = new RoleDetailViewModel
            {
                Role = response.Data
            };
            return View(viewModel);
        }

        TempData["ErrorMessage"] = response.ErrorMessage ?? "Role not found";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// GET: /Role/Create
    /// Display the role creation form.
    /// </summary>
    [HttpGet]
    public IActionResult Create()
    {
        var model = new CreateRoleRequest
        {
            CompanyId = _companyContext.SelectedCompanyId
        };
        return View(model);
    }

    /// <summary>
    /// POST: /Role/Create
    /// Process role creation form submission.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateRoleRequest model,
        CancellationToken cancellationToken)
    {
        // Auto-fill CompanyId from global context if not set
        model.CompanyId ??= _companyContext.SelectedCompanyId;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var response = await _identityClient.CreateRoleAsync(model, cancellationToken);

        if (response.IsSuccess && response.Data != null)
        {
            TempData["SuccessMessage"] = $"Role '{response.Data.Name}' created successfully!";
            return RedirectToAction(nameof(Details), new { id = response.Data.Id });
        }

        if (response.ValidationErrors != null)
        {
            foreach (var (field, errors) in response.ValidationErrors)
            {
                foreach (var error in errors)
                {
                    ModelState.AddModelError(field, error);
                }
            }
        }
        else
        {
            ModelState.AddModelError(string.Empty, response.ErrorMessage ?? "Failed to create role");
        }

        return View(model);
    }

    /// <summary>
    /// GET: /Role/Edit/{id}
    /// Display the role edit form.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var response = await _identityClient.GetRoleByIdAsync(id, cancellationToken);

        if (response.IsSuccess && response.Data != null)
        {
            var model = new UpdateRoleRequest
            {
                Name = response.Data.Name,
                Description = response.Data.Description,
                IsActive = response.Data.IsActive
            };

            ViewBag.RoleId = id;
            ViewBag.RoleType = response.Data.RoleType;
            ViewBag.CompanyId = response.Data.CompanyId;
            return View(model);
        }

        TempData["ErrorMessage"] = response.ErrorMessage ?? "Role not found";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// POST: /Role/Edit/{id}
    /// Process role edit form submission.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        Guid id,
        UpdateRoleRequest model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.RoleId = id;
            return View(model);
        }

        var response = await _identityClient.UpdateRoleAsync(id, model, cancellationToken);

        if (response.IsSuccess && response.Data != null)
        {
            TempData["SuccessMessage"] = $"Role '{response.Data.Name}' updated successfully!";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (response.ValidationErrors != null)
        {
            foreach (var (field, errors) in response.ValidationErrors)
            {
                foreach (var error in errors)
                {
                    ModelState.AddModelError(field, error);
                }
            }
        }
        else
        {
            ModelState.AddModelError(string.Empty, response.ErrorMessage ?? "Failed to update role");
        }

        ViewBag.RoleId = id;
        return View(model);
    }

    /// <summary>
    /// POST: /Role/Delete/{id}
    /// Delete a role.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var response = await _identityClient.DeleteRoleAsync(id, cancellationToken);

        if (response.IsSuccess)
        {
            TempData["SuccessMessage"] = "Role deleted successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to delete role";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// GET: /Role/Permissions/{id}
    /// Display permission assignment page for a role.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Permissions(Guid id, CancellationToken cancellationToken)
    {
        var roleResponse = await _identityClient.GetRoleByIdAsync(id, cancellationToken);
        var catalogResponse = await _identityClient.GetPermissionCatalogAsync(cancellationToken);

        if (!roleResponse.IsSuccess || roleResponse.Data == null)
        {
            TempData["ErrorMessage"] = roleResponse.ErrorMessage ?? "Role not found";
            return RedirectToAction(nameof(Index));
        }

        var viewModel = new RoleDetailViewModel
        {
            Role = roleResponse.Data,
            PermissionCatalog = catalogResponse.IsSuccess ? catalogResponse.Data : null
        };

        return View(viewModel);
    }

    /// <summary>
    /// POST: /Role/Permissions/{id}
    /// Assign permissions to a role.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Permissions(
        Guid id,
        AssignPermissionsRequest model,
        CancellationToken cancellationToken)
    {
        var response = await _identityClient.AssignPermissionsToRoleAsync(id, model, cancellationToken);

        if (response.IsSuccess)
        {
            TempData["SuccessMessage"] = "Permissions updated successfully!";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to update permissions";
        return RedirectToAction(nameof(Permissions), new { id });
    }
}
