using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Application.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Attendance.Application.Abstractions;

namespace HRM.Modules.Attendance.Application.Commands.UpdateLeaveApprovalSettings;

internal sealed class UpdateLeaveApprovalSettingsCommandHandler
    : ICommandHandler<UpdateLeaveApprovalSettingsCommand>
{
    private readonly ILeaveApprovalSettingRepository _repository;
    private readonly ITenantContext _tenantContext;

    public UpdateLeaveApprovalSettingsCommandHandler(
        ILeaveApprovalSettingRepository repository,
        ITenantContext tenantContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
    }

    public async Task<Result> Handle(
        UpdateLeaveApprovalSettingsCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId
            ?? throw new InvalidOperationException("TenantId is required.");

        var setting = await _repository.GetByTenantIdAsync(tenantId, cancellationToken);

        if (setting is null)
        {
            setting = Domain.Entities.LeaveApprovalSetting.CreateDefault(tenantId);
            setting.Update(
                request.RequiresApproval,
                request.MaxApprovalLevels,
                request.AutoApproveIfDaysLessThanOrEqual,
                request.AllowSelfCancel,
                request.NotifyOnDecision);
            _repository.Add(setting);
        }
        else
        {
            setting.Update(
                request.RequiresApproval,
                request.MaxApprovalLevels,
                request.AutoApproveIfDaysLessThanOrEqual,
                request.AllowSelfCancel,
                request.NotifyOnDecision);
            _repository.Update(setting);
        }

        return Result.Success();
    }
}
