using HRM.BuildingBlocks.Infrastructure.BackgroundServices;
using HRM.BuildingBlocks.Infrastructure.Persistence;
using HRM.Modules.Organization.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HRM.Modules.Organization.Infrastructure.BackgroundServices;

/// <summary>
/// Background service for processing Organization module's outbox messages.
/// Polls Organization.OutboxMessages table periodically, deserializes integration events,
/// and dispatches them to IIntegrationEventHandler&lt;T&gt; resolved from the DI container.
///
/// Distributed Locking:
/// - Lock resource: "OutboxProcessor_Organization"
/// - Ensures only ONE instance processes outbox at a time
/// - Uses SQL Server sp_getapplock (no external dependencies)
/// </summary>
public sealed class OrganizationOutboxProcessor : OutboxProcessor
{
    public OrganizationOutboxProcessor(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<OutboxProcessor> logger,
        IOptions<OutboxSettings> options)
        : base(serviceScopeFactory, logger, options.Value)
    {
    }

    /// <summary>
    /// Get OrganizationDbContext from service provider.
    /// </summary>
    protected override ModuleDbContext GetDbContext(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<OrganizationDbContext>();
    }
}
