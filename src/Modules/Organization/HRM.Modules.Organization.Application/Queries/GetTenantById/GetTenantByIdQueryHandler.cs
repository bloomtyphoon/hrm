using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Application.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Domain.Abstractions.Multitenancy;
using HRM.Modules.Organization.Application.DTOs;
using HRM.Modules.Organization.Domain.Entities;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Queries.GetTenantById;

internal sealed class GetTenantByIdQueryHandler : IQueryHandler<GetTenantByIdQuery, TenantDto?>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContext _tenantContext;

    public GetTenantByIdQueryHandler(ITenantRepository tenantRepository, ITenantContext tenantContext)
    {
        _tenantRepository = tenantRepository;
        _tenantContext = tenantContext;
    }

    public async Task<TenantDto?> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
    {
        // Defense-in-depth: only system tenant users may read individual tenants.
        if (_tenantContext.TenantId != WellKnownTenants.SystemTenantId)
            return null;

        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        return tenant is null ? null : MapToDto(tenant);
    }

    private static TenantDto MapToDto(Tenant tenant) => new(
        Id: tenant.Id,
        Code: tenant.Code,
        Name: tenant.Name,
        Status: tenant.Status.ToString(),
        IsSystemTenant: tenant.IsSystemTenant,
        Subdomain: tenant.Subdomain,
        CreatedAtUtc: tenant.CreatedAtUtc,
        ModifiedAtUtc: tenant.ModifiedAtUtc);
}
