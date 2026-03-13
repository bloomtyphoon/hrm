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

namespace HRM.Modules.Personnel.Application.Commands.RemoveManager;

internal sealed class RemoveManagerCommandHandler : ICommandHandler<RemoveManagerCommand>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;
    private readonly IPersonnelQueryContext _queryContext;

    public RemoveManagerCommandHandler(
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

    public async Task<Result> Handle(RemoveManagerCommand request, CancellationToken cancellationToken)
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

        employee.RemoveManager();
        _employeeRepository.Update(employee);

        return Result.Success();
    }

}
