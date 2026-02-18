using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.Modules.Organization.Application.DTOs;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Queries.GetDepartmentsByCompany;

internal sealed class GetDepartmentsByCompanyQueryHandler
    : IQueryHandler<GetDepartmentsByCompanyQuery, IReadOnlyList<DepartmentDto>>
{
    private readonly IDepartmentRepository _departmentRepository;

    public GetDepartmentsByCompanyQueryHandler(IDepartmentRepository departmentRepository)
    {
        _departmentRepository = departmentRepository;
    }

    public async Task<IReadOnlyList<DepartmentDto>> Handle(
        GetDepartmentsByCompanyQuery request,
        CancellationToken cancellationToken)
    {
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
