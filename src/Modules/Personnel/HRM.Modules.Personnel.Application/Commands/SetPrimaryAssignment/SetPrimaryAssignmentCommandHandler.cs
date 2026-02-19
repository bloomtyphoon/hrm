using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Personnel.Application.Abstractions;
using HRM.Modules.Personnel.Domain.Errors;

namespace HRM.Modules.Personnel.Application.Commands.SetPrimaryAssignment;

internal sealed class SetPrimaryAssignmentCommandHandler : ICommandHandler<SetPrimaryAssignmentCommand>
{
    private readonly IEmployeeRepository _employeeRepository;

    public SetPrimaryAssignmentCommandHandler(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public async Task<Result> Handle(SetPrimaryAssignmentCommand request, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetWithAssignmentsAsync(request.EmployeeId, cancellationToken);
        if (employee is null)
        {
            return Result.Failure(EmployeeErrors.NotFound(request.EmployeeId));
        }

        try
        {
            employee.SetAsPrimaryAssignment(request.AssignmentId);
        }
        catch (InvalidOperationException)
        {
            return Result.Failure(EmployeeErrors.AssignmentNotFound(request.AssignmentId));
        }

        _employeeRepository.Update(employee);
        return Result.Success();
    }
}
