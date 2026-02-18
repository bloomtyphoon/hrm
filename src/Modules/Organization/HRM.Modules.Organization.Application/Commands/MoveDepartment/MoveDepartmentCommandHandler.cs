using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Organization.Domain.Errors;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Commands.MoveDepartment;

internal sealed class MoveDepartmentCommandHandler : ICommandHandler<MoveDepartmentCommand>
{
    private readonly IDepartmentRepository _departmentRepository;

    public MoveDepartmentCommandHandler(IDepartmentRepository departmentRepository)
    {
        _departmentRepository = departmentRepository;
    }

    public async Task<Result> Handle(MoveDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(request.DepartmentId, cancellationToken);
        if (department is null)
        {
            return Result.Failure(DepartmentErrors.NotFound(request.DepartmentId));
        }

        if (request.NewParentDepartmentId.HasValue)
        {
            var newParent = await _departmentRepository.GetByIdAsync(request.NewParentDepartmentId.Value, cancellationToken);
            if (newParent is null)
            {
                return Result.Failure(DepartmentErrors.ParentNotFound(request.NewParentDepartmentId.Value));
            }

            if (newParent.CompanyId != department.CompanyId)
            {
                return Result.Failure(DepartmentErrors.ParentInDifferentCompany(newParent.Id, department.CompanyId));
            }

            department.MoveTo(newParent);
        }
        else
        {
            department.MoveTo(null);
        }

        _departmentRepository.Update(department);

        return Result.Success();
    }
}
