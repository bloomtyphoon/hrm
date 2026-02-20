using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Personnel.Application.Abstractions;
using HRM.Modules.Personnel.Domain.Errors;

namespace HRM.Modules.Personnel.Application.Commands.UpdateEmployee;

internal sealed class UpdateEmployeeCommandHandler : ICommandHandler<UpdateEmployeeCommand>
{
    private readonly IEmployeeRepository _employeeRepository;

    public UpdateEmployeeCommandHandler(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public async Task<Result> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee is null)
        {
            return Result.Failure(EmployeeErrors.NotFound(request.EmployeeId));
        }

        // Check email uniqueness if changed
        if (!employee.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existingByEmail = await _employeeRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (existingByEmail is not null && existingByEmail.Id != request.EmployeeId)
            {
                return Result.Failure(EmployeeErrors.EmailAlreadyExists(request.Email));
            }
        }

        employee.UpdatePersonalInfo(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Phone,
            request.DateOfBirth);

        _employeeRepository.Update(employee);
        return Result.Success();
    }
}
