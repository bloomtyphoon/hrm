using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.Modules.Organization.Application.DTOs;
using HRM.Modules.Organization.Domain.Entities;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Queries.GetTenantById;

internal sealed class GetTenantByIdQueryHandler : IQueryHandler<GetTenantByIdQuery, TenantDto?>
{
    private readonly ITenantRepository _tenantRepository;

    public GetTenantByIdQueryHandler(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<TenantDto?> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        return tenant is null ? null : MapToDto(tenant);
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
