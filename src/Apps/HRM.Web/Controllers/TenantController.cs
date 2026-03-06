using HRM.Web.Models;
using HRM.Web.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Web.Controllers;

[Authorize]
public class TenantController : Controller
{
    private readonly IOrganizationApiClient _organizationClient;
    private readonly ILogger<TenantController> _logger;

    public TenantController(
        IOrganizationApiClient organizationClient,
        ILogger<TenantController> logger)
    {
        _organizationClient = organizationClient;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? searchTerm, string? status)
    {
        var result = await _organizationClient.GetTenantsAsync();

        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Failed to load tenants.";
            return View(new TenantListViewModel());
        }

        var tenants = result.Data ?? [];

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            tenants = tenants
                .Where(t => t.Code.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                         || t.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            tenants = tenants
                .Where(t => t.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var viewModel = new TenantListViewModel
        {
            Tenants = tenants,
            SearchTerm = searchTerm,
            StatusFilter = status
        };

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateTenantRequest());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTenantRequest model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _organizationClient.CreateTenantAsync(model);

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = $"Tenant '{model.Code}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        if (result.ValidationErrors is { Count: > 0 })
        {
            foreach (var kvp in result.ValidationErrors)
                foreach (var msg in kvp.Value)
                    ModelState.AddModelError(kvp.Key, msg);
        }
        else
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Failed to create tenant.");
        }

        return View(model);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var result = await _organizationClient.GetTenantByIdAsync(id);

        if (!result.IsSuccess || result.Data is null)
        {
            TempData["ErrorMessage"] = "Tenant not found.";
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var result = await _organizationClient.GetTenantByIdAsync(id);

        if (!result.IsSuccess || result.Data is null)
        {
            TempData["ErrorMessage"] = "Tenant not found.";
            return RedirectToAction(nameof(Index));
        }

        var tenant = result.Data;
        var model = new EditTenantFormModel
        {
            Id = tenant.Id,
            Code = tenant.Code,
            Name = tenant.Name,
            Status = tenant.Status,
            IsSystemTenant = tenant.IsSystemTenant
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, EditTenantFormModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _organizationClient.UpdateTenantAsync(id, model.Name);

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = "Tenant updated successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }

        ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Failed to update tenant.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(Guid id)
    {
        var result = await _organizationClient.ActivateTenantAsync(id);

        TempData[result.IsSuccess ? "SuccessMessage" : "ErrorMessage"] =
            result.IsSuccess ? "Tenant activated successfully." : (result.ErrorMessage ?? "Failed to activate tenant.");

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Suspend(Guid id)
    {
        var result = await _organizationClient.SuspendTenantAsync(id);

        TempData[result.IsSuccess ? "SuccessMessage" : "ErrorMessage"] =
            result.IsSuccess ? "Tenant suspended successfully." : (result.ErrorMessage ?? "Failed to suspend tenant.");

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var result = await _organizationClient.DeactivateTenantAsync(id);

        TempData[result.IsSuccess ? "SuccessMessage" : "ErrorMessage"] =
            result.IsSuccess ? "Tenant deactivated successfully." : (result.ErrorMessage ?? "Failed to deactivate tenant.");

        return RedirectToAction(nameof(Details), new { id });
    }
}
