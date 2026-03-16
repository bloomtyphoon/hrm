using HRM.Modules.Identity.Domain.Entities;
using HRM.Modules.Identity.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HRM.Modules.Identity.Infrastructure.Services;

/// <summary>
/// Loads tenant scope overrides from the database.
///
/// Uses IServiceScopeFactory to create a scoped DbContext since
/// PermissionCatalogService is Singleton and cannot depend on scoped services directly.
/// </summary>
public sealed class TenantScopeOverrideProvider : ITenantScopeOverrideProvider
{
    private readonly IServiceScopeFactory _scopeFactory;

    public TenantScopeOverrideProvider(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    }

    public async Task<List<TenantScopeOverride>> GetOverridesAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<Persistence.IdentityDbContext>();

        return await dbContext.TenantScopeOverrides
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId)
            .ToListAsync(cancellationToken);
    }
}
