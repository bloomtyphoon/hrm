using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Personnel.Application.Abstractions;
using HRM.Modules.Personnel.Application.Security;
using HRM.Modules.Personnel.Domain.Entities;
using HRM.Modules.Personnel.Domain.Errors;

namespace HRM.Modules.Personnel.Application.Commands.AddAssignment;

internal sealed class AddAssignmentCommandHandler : ICommandHandler<AddAssignmentCommand, Guid>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;

    public AddAssignmentCommandHandler(
        IEmployeeRepository employeeRepository,
        IDataScopeService dataScopeService,
        IExecutionContext executionContext)
    {
        _employeeRepository = employeeRepository;
        _dataScopeService = dataScopeService;
        _executionContext = executionContext;
    }

    public async Task<Result<Guid>> Handle(AddAssignmentCommand request, CancellationToken cancellationToken)
    {
        // 1. Load employee with existing assignments
        var employee = await _employeeRepository.GetWithAssignmentsAsync(request.EmployeeId, cancellationToken);
        if (employee is null)
        {
            return Result.Failure<Guid>(EmployeeErrors.NotFound(request.EmployeeId));
        }

        var rule = await _dataScopeService.GetScopeRuleAsync(
            _executionContext.UserId, PersonnelPermissions.Assignment.Create, cancellationToken);

        if (!CanAccessEmployee(employee, rule))
        {
            return Result.Failure<Guid>(new ForbiddenError(
                "Assignment.AccessDenied", "You do not have permission to add assignments to this employee."));
        }

        // 2. Add assignment via aggregate root
        var assignment = employee.AddAssignment(
            companyId: request.CompanyId,
            departmentId: request.DepartmentId,
            positionId: request.PositionId,
            startDate: request.StartDate,
            isPrimary: request.IsPrimary
        );

        // 3. Update employee
        _employeeRepository.Update(employee);

        return Result.Success(assignment.Id);
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
