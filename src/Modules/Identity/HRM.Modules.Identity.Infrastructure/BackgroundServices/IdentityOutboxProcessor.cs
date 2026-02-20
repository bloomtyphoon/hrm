using HRM.BuildingBlocks.Infrastructure.BackgroundServices;
using HRM.BuildingBlocks.Infrastructure.Persistence;
using HRM.Modules.Identity.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HRM.Modules.Identity.Infrastructure.BackgroundServices;

/// <summary>
/// Background service for processing Identity module's outbox messages.
/// Configuration is read from appsettings.json "OutboxSettings" section.
///
/// Responsibilities:
/// - Poll Identity.OutboxMessages table periodically
/// - Deserialize and publish integration events
/// - Mark messages as processed or failed
/// - Implement retry logic with configurable max attempts
/// - Use distributed locking for scaled deployments
///
/// Distributed Locking:
/// - Lock resource: "OutboxProcessor_Identity"
/// - Ensures only ONE instance processes outbox at a time
/// - Uses SQL Server sp_getapplock (no external dependencies)
/// </summary>
public sealed class IdentityOutboxProcessor : OutboxProcessor
{
    public IdentityOutboxProcessor(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<OutboxProcessor> logger,
        IOptions<OutboxSettings> options)
        : base(serviceScopeFactory, logger, options.Value)
    {
    }

    /// <summary>
    /// Get IdentityDbContext from service provider.
    /// </summary>
    protected override ModuleDbContext GetDbContext(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<IdentityDbContext>();
    }
}
