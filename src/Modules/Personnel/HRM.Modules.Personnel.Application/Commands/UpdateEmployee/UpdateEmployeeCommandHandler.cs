using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Personnel.Application.Abstractions;
using HRM.Modules.Personnel.Application.Abstractions.Data;
using HRM.Modules.Personnel.Application.Security;
using HRM.Modules.Personnel.Domain.Entities;
using HRM.Modules.Personnel.Domain.Errors;

namespace HRM.Modules.Personnel.Application.Commands.UpdateEmployee;

internal sealed class UpdateEmployeeCommandHandler : ICommandHandler<UpdateEmployeeCommand>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;
    private readonly IPersonnelQueryContext _queryContext;

    public UpdateEmployeeCommandHandler(
        IEmployeeRepository employeeRepository,
        IDataScopeService dataScopeService,
        IExecutionContext executionContext,
        IPersonnelQueryContext queryContext)
    {
        _employeeRepository = employeeRepository;
        _dataScopeService = dataScopeService;
        _executionContext = executionContext;
        _queryContext = queryContext;
    }

    public async Task<Result> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee is null)
        {
            return Result.Failure(EmployeeErrors.NotFound(request.EmployeeId));
        }

        var rule = await _dataScopeService.GetScopeRuleAsync(
            _executionContext.UserId, PersonnelPermissions.Employee.Update, cancellationToken);

        if (!await EmployeeScopeFilter.IsAccessibleAsync(rule, employee.Id, _queryContext, cancellationToken))
        {
            return Result.Failure(new ForbiddenError(
                "Employee.AccessDenied", "You do not have permission to update this employee."));
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
