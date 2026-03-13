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

namespace HRM.Modules.Personnel.Application.Commands.EndAssignment;

internal sealed class EndAssignmentCommandHandler : ICommandHandler<EndAssignmentCommand>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;
    private readonly IPersonnelQueryContext _queryContext;

    public EndAssignmentCommandHandler(
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

    public async Task<Result> Handle(EndAssignmentCommand request, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetWithAssignmentsAsync(request.EmployeeId, cancellationToken);
        if (employee is null)
        {
            return Result.Failure(EmployeeErrors.NotFound(request.EmployeeId));
        }

        var rule = await _dataScopeService.GetScopeRuleAsync(
            _executionContext.UserId, PersonnelPermissions.Assignment.Update, cancellationToken);

        if (!await EmployeeScopeFilter.IsAccessibleAsync(rule, employee.Id, _queryContext, cancellationToken))
        {
            return Result.Failure(new ForbiddenError(
                "Assignment.AccessDenied", "You do not have permission to end assignments for this employee."));
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
