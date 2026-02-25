using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Personnel.Application.Abstractions;
using HRM.Modules.Personnel.Domain.Errors;

namespace HRM.Modules.Personnel.Application.Commands.RemoveManager;

internal sealed class RemoveManagerCommandHandler : ICommandHandler<RemoveManagerCommand>
{
    private readonly IEmployeeRepository _employeeRepository;

    public RemoveManagerCommandHandler(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public async Task<Result> Handle(RemoveManagerCommand request, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee is null)
        {
            return Result.Failure(EmployeeErrors.NotFound(request.EmployeeId));
        }

        employee.RemoveManager();
        _employeeRepository.Update(employee);

        return Result.Success();
    }
}
