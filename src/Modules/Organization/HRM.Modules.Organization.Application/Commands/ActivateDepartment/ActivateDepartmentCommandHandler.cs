using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Organization.Domain.Errors;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Commands.ActivateDepartment;

internal sealed class ActivateDepartmentCommandHandler : ICommandHandler<ActivateDepartmentCommand>
{
    private readonly IDepartmentRepository _departmentRepository;

    public ActivateDepartmentCommandHandler(IDepartmentRepository departmentRepository)
    {
        _departmentRepository = departmentRepository;
    }

    public async Task<Result> Handle(ActivateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(request.DepartmentId, cancellationToken);
        if (department is null)
        {
            return Result.Failure(DepartmentErrors.NotFound(request.DepartmentId));
        }

        department.Activate();
        _departmentRepository.Update(department);

        return Result.Success();
    }
}
