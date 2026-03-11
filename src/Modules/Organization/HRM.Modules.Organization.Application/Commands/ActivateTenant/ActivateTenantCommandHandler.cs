using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Application.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Domain.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Organization.Domain.Entities;
using HRM.Modules.Organization.Domain.Errors;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Commands.ActivateTenant;

internal sealed class ActivateTenantCommandHandler : ICommandHandler<ActivateTenantCommand>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContext _tenantContext;

    public ActivateTenantCommandHandler(ITenantRepository tenantRepository, ITenantContext tenantContext)
    {
        _tenantRepository = tenantRepository;
        _tenantContext = tenantContext;
    }

    public async Task<Result> Handle(ActivateTenantCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId != WellKnownTenants.SystemTenantId)
            return Result.Failure(TenantErrors.SystemTenantAccessOnly());

        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant is null)
            return Result.Failure(TenantErrors.NotFound(request.TenantId));

        if (tenant.Status == TenantStatus.Active)
            return Result.Failure(TenantErrors.AlreadyActive(tenant.Code));

        tenant.Activate();
        _tenantRepository.Update(tenant);

        return Result.Success();
    }
}
