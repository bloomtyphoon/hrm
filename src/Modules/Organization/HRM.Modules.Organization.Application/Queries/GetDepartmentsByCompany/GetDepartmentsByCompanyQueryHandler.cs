using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.Modules.Organization.Application.DTOs;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Queries.GetDepartmentsByCompany;

internal sealed class GetDepartmentsByCompanyQueryHandler
    : IQueryHandler<GetDepartmentsByCompanyQuery, IReadOnlyList<DepartmentDto>>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IExecutionContext _executionContext;

    public GetDepartmentsByCompanyQueryHandler(
        IDepartmentRepository departmentRepository,
        IExecutionContext executionContext)
    {
        _departmentRepository = departmentRepository;
        _executionContext = executionContext;
    }

    public async Task<IReadOnlyList<DepartmentDto>> Handle(
        GetDepartmentsByCompanyQuery request,
        CancellationToken cancellationToken)
    {
        // Employee accounts can only query departments of their own company
        var accountType = _executionContext.GetClaimValue("AccountType");
        if (accountType == "Employee")
        {
            var companyIdClaim = _executionContext.GetClaimValue("CompanyId");
            if (!Guid.TryParse(companyIdClaim, out var employeeCompanyId))
                return Array.Empty<DepartmentDto>();

            if (request.CompanyId != employeeCompanyId)
                return Array.Empty<DepartmentDto>();
        }

        var departments = await _departmentRepository.GetByCompanyIdAsync(request.CompanyId, cancellationToken);

        return departments.Select(d => new DepartmentDto(
            Id: d.Id,
            Code: d.Code,
            Name: d.Name,
            CompanyId: d.CompanyId,
            ParentDepartmentId: d.ParentDepartmentId,
            ManagerId: d.ManagerId,
            Level: d.Level,
            Status: d.Status.ToString(),
            CreatedAtUtc: d.CreatedAtUtc,
            ModifiedAtUtc: d.ModifiedAtUtc
        )).ToList();
    }
}
