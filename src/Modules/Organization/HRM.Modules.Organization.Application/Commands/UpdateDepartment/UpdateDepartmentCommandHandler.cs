using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Organization.Domain.Errors;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Commands.UpdateDepartment;

internal sealed class UpdateDepartmentCommandHandler : ICommandHandler<UpdateDepartmentCommand>
{
    private readonly IDepartmentRepository _departmentRepository;

    public UpdateDepartmentCommandHandler(IDepartmentRepository departmentRepository)
    {
        _departmentRepository = departmentRepository;
    }

    public async Task<Result> Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(request.DepartmentId, cancellationToken);
        if (department is null)
        {
            return Result.Failure(DepartmentErrors.NotFound(request.DepartmentId));
        }

        department.Update(request.Name, request.ManagerId);
        _departmentRepository.Update(department);

        return Result.Success();
    }
}
