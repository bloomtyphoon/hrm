using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.Modules.Organization.Application.DTOs;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Queries.GetDepartmentById;

internal sealed class GetDepartmentByIdQueryHandler : IQueryHandler<GetDepartmentByIdQuery, DepartmentDto?>
{
    private readonly IDepartmentRepository _departmentRepository;

    public GetDepartmentByIdQueryHandler(IDepartmentRepository departmentRepository)
    {
        _departmentRepository = departmentRepository;
    }

    public async Task<DepartmentDto?> Handle(GetDepartmentByIdQuery request, CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(request.DepartmentId, cancellationToken);

        if (department is null)
            return null;

        return new DepartmentDto(
            Id: department.Id,
            Code: department.Code,
            Name: department.Name,
            CompanyId: department.CompanyId,
            ParentDepartmentId: department.ParentDepartmentId,
            ManagerId: department.ManagerId,
            Level: department.Level,
            Status: department.Status.ToString(),
            CreatedAtUtc: department.CreatedAtUtc,
            ModifiedAtUtc: department.ModifiedAtUtc
        );
    }
}
