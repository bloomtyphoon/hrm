using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Organization.Domain.Errors;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Commands.DeactivateDepartment;

internal sealed class DeactivateDepartmentCommandHandler : ICommandHandler<DeactivateDepartmentCommand>
{
    private readonly IDepartmentRepository _departmentRepository;

    public DeactivateDepartmentCommandHandler(IDepartmentRepository departmentRepository)
    {
        _departmentRepository = departmentRepository;
    }

    public async Task<Result> Handle(DeactivateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(request.DepartmentId, cancellationToken);
        if (department is null)
        {
            return Result.Failure(DepartmentErrors.NotFound(request.DepartmentId));
        }

        department.Deactivate();
        _departmentRepository.Update(department);

        return Result.Success();
    }
}
