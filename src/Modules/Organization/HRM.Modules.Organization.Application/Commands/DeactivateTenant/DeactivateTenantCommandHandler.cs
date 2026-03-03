using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Application.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Domain.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Organization.Domain.Entities;
using HRM.Modules.Organization.Domain.Errors;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Commands.DeactivateTenant;

internal sealed class DeactivateTenantCommandHandler : ICommandHandler<DeactivateTenantCommand>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContext _tenantContext;

    public DeactivateTenantCommandHandler(ITenantRepository tenantRepository, ITenantContext tenantContext)
    {
        _tenantRepository = tenantRepository;
        _tenantContext = tenantContext;
    }

    public async Task<Result> Handle(DeactivateTenantCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId != WellKnownTenants.SystemTenantId)
            return Result.Failure(TenantErrors.SystemTenantAccessOnly());

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
