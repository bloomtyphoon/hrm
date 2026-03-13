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

namespace HRM.Modules.Personnel.Application.Commands.AssignManager;

internal sealed class AssignManagerCommandHandler : ICommandHandler<AssignManagerCommand>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;
    private readonly IPersonnelQueryContext _queryContext;

    public AssignManagerCommandHandler(
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

    public async Task<Result> Handle(AssignManagerCommand request, CancellationToken cancellationToken)
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
                "Employee.AccessDenied", "You do not have permission to manage this employee."));
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
