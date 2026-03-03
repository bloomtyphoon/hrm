using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Application.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Domain.Abstractions.Multitenancy;
using HRM.Modules.Organization.Application.DTOs;
using HRM.Modules.Organization.Domain.Entities;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Queries.GetTenants;

internal sealed class GetTenantsQueryHandler : IQueryHandler<GetTenantsQuery, IReadOnlyList<TenantDto>>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContext _tenantContext;

    public GetTenantsQueryHandler(ITenantRepository tenantRepository, ITenantContext tenantContext)
    {
        _tenantRepository = tenantRepository;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<TenantDto>> Handle(GetTenantsQuery request, CancellationToken cancellationToken)
    {
        // Defense-in-depth: only system tenant users may list tenants.
        // Primary gate is RouteSecurityMap (Organization.Tenant.View with Global scope).
        if (_tenantContext.TenantId != WellKnownTenants.SystemTenantId)
            return Array.Empty<TenantDto>();

        var tenants = await _tenantRepository.GetAllAsync(cancellationToken);
        return tenants.Select(MapToDto).ToList();
    }

    private static TenantDto MapToDto(Tenant tenant) => new(
        Id: tenant.Id,
        Code: tenant.Code,
        Name: tenant.Name,
        Status: tenant.Status.ToString(),
        IsSystemTenant: tenant.IsSystemTenant,
        CreatedAtUtc: tenant.CreatedAtUtc,
        ModifiedAtUtc: tenant.ModifiedAtUtc);
}
