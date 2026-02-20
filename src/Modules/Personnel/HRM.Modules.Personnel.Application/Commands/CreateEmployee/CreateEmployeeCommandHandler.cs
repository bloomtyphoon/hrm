using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Personnel.Application.Abstractions;
using HRM.Modules.Personnel.Domain.Entities;
using HRM.Modules.Personnel.Domain.Errors;

namespace HRM.Modules.Personnel.Application.Commands.CreateEmployee;

internal sealed class CreateEmployeeCommandHandler : ICommandHandler<CreateEmployeeCommand, Guid>
{
    private readonly IEmployeeRepository _employeeRepository;

    public CreateEmployeeCommandHandler(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public async Task<Result<Guid>> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        // 1. Check employee code uniqueness
        var existingByCode = await _employeeRepository.GetByCodeAsync(request.EmployeeCode, cancellationToken);
        if (existingByCode is not null)
        {
            return Result.Failure<Guid>(EmployeeErrors.CodeAlreadyExists(request.EmployeeCode));
        }

        // 2. Check email uniqueness
        var existingByEmail = await _employeeRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingByEmail is not null)
        {
            return Result.Failure<Guid>(EmployeeErrors.EmailAlreadyExists(request.Email));
        }

        // 3. Verify manager exists (if specified)
        if (request.ManagerId.HasValue)
        {
            var manager = await _employeeRepository.GetByIdAsync(request.ManagerId.Value, cancellationToken);
            if (manager is null)
            {
                return Result.Failure<Guid>(EmployeeErrors.NotFound(request.ManagerId.Value));
            }
        }

        // 4. Create Employee aggregate
        var employee = Employee.Create(
            employeeCode: request.EmployeeCode,
            firstName: request.FirstName,
            lastName: request.LastName,
            email: request.Email,
            hireDate: request.HireDate,
            phone: request.Phone,
            dateOfBirth: request.DateOfBirth,
            managerId: request.ManagerId
        );

        // 5. Add to repository
        _employeeRepository.Add(employee);

        return Result.Success(employee.Id);
    }
}
