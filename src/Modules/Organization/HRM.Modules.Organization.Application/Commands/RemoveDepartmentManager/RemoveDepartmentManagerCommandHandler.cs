using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Organization.Domain.Errors;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Commands.RemoveDepartmentManager;

internal sealed class RemoveDepartmentManagerCommandHandler : ICommandHandler<RemoveDepartmentManagerCommand>
{
    private readonly IDepartmentRepository _departmentRepository;

    public RemoveDepartmentManagerCommandHandler(IDepartmentRepository departmentRepository)
    {
        _departmentRepository = departmentRepository;
    }

    public async Task<Result> Handle(RemoveDepartmentManagerCommand request, CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(request.DepartmentId, cancellationToken);
        if (department is null)
        {
            return Result.Failure(DepartmentErrors.NotFound(request.DepartmentId));
        }

        department.RemoveManager();
        _departmentRepository.Update(department);

        return Result.Success();
    }
}
