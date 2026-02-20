using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Personnel.Application.Abstractions;
using HRM.Modules.Personnel.Domain.Errors;

namespace HRM.Modules.Personnel.Application.Commands.AssignManager;

internal sealed class AssignManagerCommandHandler : ICommandHandler<AssignManagerCommand>
{
    private readonly IEmployeeRepository _employeeRepository;

    public AssignManagerCommandHandler(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public async Task<Result> Handle(AssignManagerCommand request, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee is null)
        {
            return Result.Failure(EmployeeErrors.NotFound(request.EmployeeId));
        }

        var manager = await _employeeRepository.GetByIdAsync(request.ManagerId, cancellationToken);
        if (manager is null)
        {
            return Result.Failure(EmployeeErrors.NotFound(request.ManagerId));
        }

        // Check for circular reference
        var isSubordinate = await _employeeRepository.IsSubordinateOfAsync(
            request.ManagerId, request.EmployeeId, cancellationToken);
        if (isSubordinate)
        {
            return Result.Failure(new ValidationError(
                "Employee.CircularManagerReference",
                "Cannot assign a subordinate as manager (circular reference)."));
        }

        employee.AssignManager(request.ManagerId);
        _employeeRepository.Update(employee);

        return Result.Success();
    }
}
