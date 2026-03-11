using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Application.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Domain.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Organization.Domain.Errors;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Commands.UpdateTenant;

internal sealed class UpdateTenantCommandHandler : ICommandHandler<UpdateTenantCommand>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContext _tenantContext;

    public UpdateTenantCommandHandler(ITenantRepository tenantRepository, ITenantContext tenantContext)
    {
        _tenantRepository = tenantRepository;
        _tenantContext = tenantContext;
    }

    public async Task<Result> Handle(UpdateTenantCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId != WellKnownTenants.SystemTenantId)
            return Result.Failure(TenantErrors.SystemTenantAccessOnly());

        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant is null)
            return Result.Failure(TenantErrors.NotFound(request.TenantId));

        tenant.Update(request.Name);

        // Subdomain: check uniqueness only when a non-empty value is being set
        if (!string.IsNullOrWhiteSpace(request.Subdomain))
        {
            var normalized = request.Subdomain.Trim().ToLowerInvariant();
            var existing = await _tenantRepository.GetBySubdomainAsync(normalized, cancellationToken);
            if (existing is not null && existing.Id != tenant.Id)
                return Result.Failure(TenantErrors.SubdomainAlreadyExists(normalized));
        }

        tenant.UpdateSubdomain(request.Subdomain);
        _tenantRepository.Update(tenant);

        return Result.Success();
    }
}
