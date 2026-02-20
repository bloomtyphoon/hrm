using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Personnel.Application.Abstractions;
using HRM.Modules.Personnel.Domain.Errors;

namespace HRM.Modules.Personnel.Application.Commands.EndAssignment;

internal sealed class EndAssignmentCommandHandler : ICommandHandler<EndAssignmentCommand>
{
    private readonly IEmployeeRepository _employeeRepository;

    public EndAssignmentCommandHandler(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public async Task<Result> Handle(EndAssignmentCommand request, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetWithAssignmentsAsync(request.EmployeeId, cancellationToken);
        if (employee is null)
        {
            return Result.Failure(EmployeeErrors.NotFound(request.EmployeeId));
        }

        try
        {
            employee.EndAssignment(request.AssignmentId, request.EndDate);
        }
        catch (InvalidOperationException)
        {
            return Result.Failure(EmployeeErrors.AssignmentNotFound(request.AssignmentId));
        }

        _employeeRepository.Update(employee);
        return Result.Success();
    }
}
