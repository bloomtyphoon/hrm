using HRM.Web.Models;
using HRM.Web.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Web.Controllers;

/// <summary>
/// Controller for employee management.
/// Uses CompanyContext to filter employees by the selected company.
/// </summary>
[Authorize]
public class EmployeeController : Controller
{
    private readonly IPersonnelApiClient _personnelClient;
    private readonly ICompanyContext _companyContext;
    private readonly ILogger<EmployeeController> _logger;

    public EmployeeController(
        IPersonnelApiClient personnelClient,
        ICompanyContext companyContext,
        ILogger<EmployeeController> logger)
    {
        _personnelClient = personnelClient;
        _companyContext = companyContext;
        _logger = logger;
    }

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

        // Pass company filter from context; null means all companies (for system accounts)
        var companyId = _companyContext.IsAllCompanies ? null : _companyContext.SelectedCompanyId;

        var response = await _personnelClient.GetEmployeesAsync(
            searchTerm, status, companyId,
            departmentId: null,
            pageNumber, pageSize, cancellationToken);

        var viewModel = new EmployeeListViewModel
        {
            SearchTerm = searchTerm,
            StatusFilter = status,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        if (response.IsSuccess && response.Data != null)
        {
            viewModel.Employees = response.Data;
        }
        else
        {
            _logger.LogError("Failed to get employees: {ErrorMessage}", response.ErrorMessage);
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to load employees";
            viewModel.Employees = new PagedResult<EmployeeSummaryResponse>();
        }

        return View(viewModel);
    }
}
