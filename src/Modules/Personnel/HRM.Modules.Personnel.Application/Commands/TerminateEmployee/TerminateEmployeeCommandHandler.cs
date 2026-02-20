using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Personnel.Application.Abstractions;
using HRM.Modules.Personnel.Domain.Entities;
using HRM.Modules.Personnel.Domain.Errors;

namespace HRM.Modules.Personnel.Application.Commands.TerminateEmployee;

internal sealed class TerminateEmployeeCommandHandler : ICommandHandler<TerminateEmployeeCommand>
{
    private readonly IEmployeeRepository _employeeRepository;

    public TerminateEmployeeCommandHandler(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public async Task<Result> Handle(TerminateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetWithAssignmentsAsync(request.EmployeeId, cancellationToken);
        if (employee is null)
        {
            return Result.Failure(EmployeeErrors.NotFound(request.EmployeeId));
        }

        if (employee.Status == EmploymentStatus.Terminated)
        {
            return Result.Failure(EmployeeErrors.AlreadyTerminated(request.EmployeeId));
        }

        employee.Terminate(request.TerminationDate);
        _employeeRepository.Update(employee);

        return Result.Success();
    }
}
