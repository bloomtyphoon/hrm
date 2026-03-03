using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.Modules.Organization.Application.DTOs;
using HRM.Modules.Organization.Domain.Entities;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Queries.GetTenants;

internal sealed class GetTenantsQueryHandler : IQueryHandler<GetTenantsQuery, IReadOnlyList<TenantDto>>
{
    private readonly ITenantRepository _tenantRepository;

    public GetTenantsQueryHandler(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<IReadOnlyList<TenantDto>> Handle(GetTenantsQuery request, CancellationToken cancellationToken)
    {
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
