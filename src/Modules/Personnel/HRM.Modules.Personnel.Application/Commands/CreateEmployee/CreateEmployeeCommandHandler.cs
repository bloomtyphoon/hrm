using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Application.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Personnel.Application.Abstractions;
using HRM.Modules.Personnel.Application.Security;
using HRM.Modules.Personnel.Domain.Entities;
using HRM.Modules.Personnel.Domain.Errors;

namespace HRM.Modules.Personnel.Application.Commands.CreateEmployee;

internal sealed class CreateEmployeeCommandHandler : ICommandHandler<CreateEmployeeCommand, Guid>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;

    public CreateEmployeeCommandHandler(
        IEmployeeRepository employeeRepository,
        ITenantContext tenantContext,
        IDataScopeService dataScopeService,
        IExecutionContext executionContext)
    {
        _employeeRepository = employeeRepository;
        _tenantContext = tenantContext;
        _dataScopeService = dataScopeService;
        _executionContext = executionContext;
    }

    public async Task<Result<Guid>> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        // Scope check: creating an employee requires at least some scope
        var rule = await _dataScopeService.GetScopeRuleAsync(
            _executionContext.UserId, PersonnelPermissions.Employee.Create, cancellationToken);

        if (rule.Level == DataScopeLevel.None)
        {
            return Result.Failure<Guid>(new ForbiddenError(
                "Employee.AccessDenied", "You do not have permission to create employees."));
        }

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
        var tenantId = _tenantContext.TenantId
            ?? throw new InvalidOperationException("TenantId is required to create an employee.");
        var employee = Employee.Create(
            tenantId: tenantId,
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
