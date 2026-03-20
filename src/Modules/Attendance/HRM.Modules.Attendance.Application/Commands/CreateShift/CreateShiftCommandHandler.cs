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

namespace HRM.Modules.Attendance.Application.Commands.CreateShift;

internal sealed class CreateShiftCommandHandler
    : ICommandHandler<CreateShiftCommand, Guid>
{
    private readonly IShiftRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;

    public CreateShiftCommandHandler(
        IShiftRepository repository,
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
        CreateShiftCommand request,
        CancellationToken cancellationToken)
    {
        var rule = await _dataScopeService.GetScopeRuleAsync(
            _executionContext.UserId, AttendancePermissions.Shift.Manage, cancellationToken);

        if (rule.Level.Category == ScopeCategory.None)
            return Result.Failure<Guid>(ShiftErrors.ManageForbidden());

        var tenantId = _tenantContext.TenantId
            ?? throw new InvalidOperationException("TenantId is required.");

        if (await _repository.ExistsByNameAsync(request.Name, tenantId, cancellationToken: cancellationToken))
            return Result.Failure<Guid>(ShiftErrors.DuplicateName(request.Name));

        var shift = Shift.Create(
            tenantId: tenantId,
            name: request.Name,
            startTime: request.StartTime,
            endTime: request.EndTime,
            companyId: request.CompanyId,
            description: request.Description);

        _repository.Add(shift);

        return Result.Success(shift.Id);
    }
}
