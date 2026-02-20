using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Personnel.Application.Abstractions;
using HRM.Modules.Personnel.Domain.Errors;

namespace HRM.Modules.Personnel.Application.Commands.AddAssignment;

internal sealed class AddAssignmentCommandHandler : ICommandHandler<AddAssignmentCommand, Guid>
{
    private readonly IEmployeeRepository _employeeRepository;

    public AddAssignmentCommandHandler(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public async Task<Result<Guid>> Handle(AddAssignmentCommand request, CancellationToken cancellationToken)
    {
        // 1. Load employee with existing assignments
        var employee = await _employeeRepository.GetWithAssignmentsAsync(request.EmployeeId, cancellationToken);
        if (employee is null)
        {
            return Result.Failure<Guid>(EmployeeErrors.NotFound(request.EmployeeId));
        }

        // 2. Add assignment via aggregate root
        // CompanyId, DepartmentId, PositionId are weak references to Organization module
        // Validation of these IDs is the caller's responsibility (API layer or integration)
        var assignment = employee.AddAssignment(
            companyId: request.CompanyId,
            departmentId: request.DepartmentId,
            positionId: request.PositionId,
            startDate: request.StartDate,
            isPrimary: request.IsPrimary
        );

        // 3. Update employee (EF tracks changes including new assignment)
        _employeeRepository.Update(employee);

        return Result.Success(assignment.Id);
    }
}
