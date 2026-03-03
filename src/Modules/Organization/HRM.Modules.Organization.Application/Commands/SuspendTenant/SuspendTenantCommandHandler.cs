using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Organization.Domain.Entities;
using HRM.Modules.Organization.Domain.Errors;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Commands.SuspendTenant;

internal sealed class SuspendTenantCommandHandler : ICommandHandler<SuspendTenantCommand>
{
    private readonly ITenantRepository _tenantRepository;

    public SuspendTenantCommandHandler(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<Result> Handle(SuspendTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant is null)
            return Result.Failure(TenantErrors.NotFound(request.TenantId));

        if (tenant.IsSystemTenant)
            return Result.Failure(TenantErrors.SystemTenantImmutable());

        if (tenant.Status == TenantStatus.Suspended)
            return Result.Failure(TenantErrors.AlreadySuspended(tenant.Code));

        tenant.Suspend();
        _tenantRepository.Update(tenant);

        return Result.Success();
    }
}
