using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Application.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Attendance.Application.Abstractions;
using HRM.Modules.Attendance.Application.Security;
using HRM.Modules.Attendance.Domain.Entities;
using HRM.Modules.Attendance.Domain.Errors;

namespace HRM.Modules.Attendance.Application.Commands.CreateLeaveType;

internal sealed class CreateLeaveTypeCommandHandler
    : ICommandHandler<CreateLeaveTypeCommand, Guid>
{
    private readonly ILeaveTypeRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;

    public CreateLeaveTypeCommandHandler(
        ILeaveTypeRepository repository,
        ITenantContext tenantContext,
        IDataScopeService dataScopeService,
        IExecutionContext executionContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
        _dataScopeService = dataScopeService;
        _executionContext = executionContext;
    }

    public async Task<Result<Guid>> Handle(
        CreateLeaveTypeCommand request,
        CancellationToken cancellationToken)
    {
        var rule = await _dataScopeService.GetScopeRuleAsync(
            _executionContext.UserId, AttendancePermissions.Leave.ManageTypes, cancellationToken);

        if (rule.Level.Category == ScopeCategory.None)
            return Result.Failure<Guid>(LeaveErrors.ManageTypesForbidden());

        var tenantId = _tenantContext.TenantId
            ?? throw new InvalidOperationException("TenantId is required.");

        if (await _repository.ExistsByNameAsync(request.Name, tenantId, cancellationToken: cancellationToken))
            return Result.Failure<Guid>(LeaveErrors.DuplicateTypeName(request.Name));

        var leaveType = LeaveType.Create(
            tenantId: tenantId,
            name: request.Name,
            defaultDaysPerYear: request.DefaultDaysPerYear,
            isPaid: request.IsPaid,
            description: request.Description);

        _repository.Add(leaveType);

        return Result.Success(leaveType.Id);
    }
}
