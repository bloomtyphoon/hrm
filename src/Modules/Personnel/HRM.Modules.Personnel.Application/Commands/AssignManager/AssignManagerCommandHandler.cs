using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Personnel.Application.Abstractions;
using HRM.Modules.Personnel.Application.Security;
using HRM.Modules.Personnel.Domain.Entities;
using HRM.Modules.Personnel.Domain.Errors;

namespace HRM.Modules.Personnel.Application.Commands.AssignManager;

internal sealed class AssignManagerCommandHandler : ICommandHandler<AssignManagerCommand>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;

    public AssignManagerCommandHandler(
        IEmployeeRepository employeeRepository,
        IDataScopeService dataScopeService,
        IExecutionContext executionContext)
    {
        _employeeRepository = employeeRepository;
        _dataScopeService = dataScopeService;
        _executionContext = executionContext;
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

        if (!CanAccessEmployee(employee, rule))
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

    private static bool CanAccessEmployee(Employee employee, DataScopeRule rule) => rule.Level switch
    {
        DataScopeLevel.Global => true,
        DataScopeLevel.Self => employee.Id == rule.SelfEmployeeId,
        DataScopeLevel.DirectReports or DataScopeLevel.EmployeeSet =>
            rule.EmployeeIds.Contains(employee.Id),
        DataScopeLevel.Company =>
            employee.PrimaryCompanyId.HasValue && rule.DimensionIds.Contains(employee.PrimaryCompanyId.Value),
        DataScopeLevel.Department =>
            employee.PrimaryDepartmentId.HasValue && rule.DimensionIds.Contains(employee.PrimaryDepartmentId.Value),
        DataScopeLevel.Position =>
            employee.PrimaryPositionId.HasValue && rule.DimensionIds.Contains(employee.PrimaryPositionId.Value),
        _ => false
    };
}
