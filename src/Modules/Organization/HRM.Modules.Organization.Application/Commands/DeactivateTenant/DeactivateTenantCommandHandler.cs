using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Organization.Domain.Entities;
using HRM.Modules.Organization.Domain.Errors;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Commands.DeactivateTenant;

internal sealed class DeactivateTenantCommandHandler : ICommandHandler<DeactivateTenantCommand>
{
    private readonly ITenantRepository _tenantRepository;

    public DeactivateTenantCommandHandler(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<Result> Handle(DeactivateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant is null)
            return Result.Failure(TenantErrors.NotFound(request.TenantId));

        if (tenant.IsSystemTenant)
            return Result.Failure(TenantErrors.SystemTenantImmutable());

        if (tenant.Status == TenantStatus.Deactivated)
            return Result.Failure(TenantErrors.AlreadyDeactivated(tenant.Code));

        tenant.Deactivate();
        _tenantRepository.Update(tenant);

        return Result.Success();
    }
}
