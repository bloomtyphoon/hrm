using HRM.BuildingBlocks.Infrastructure.BackgroundServices;
using HRM.BuildingBlocks.Infrastructure.Persistence;
using HRM.Modules.Attendance.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HRM.Modules.Attendance.Infrastructure.BackgroundServices;

/// <summary>
/// Background service for processing Attendance module's outbox messages.
/// Polls Attendance.OutboxMessages table periodically, deserializes integration events,
/// and dispatches them to IIntegrationEventHandler&lt;T&gt; resolved from the DI container.
///
/// Distributed Locking:
/// - Lock resource: "OutboxProcessor_Attendance"
/// - Ensures only ONE instance processes outbox at a time
/// - Uses SQL Server sp_getapplock (no external dependencies)
/// </summary>
public sealed class AttendanceOutboxProcessor : OutboxProcessor
{
    public AttendanceOutboxProcessor(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<OutboxProcessor> logger,
        IOptions<OutboxSettings> options)
        : base(serviceScopeFactory, logger, options.Value)
    {
    }

    /// <summary>
    /// Get AttendanceDbContext from service provider.
    /// </summary>
    protected override ModuleDbContext GetDbContext(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<AttendanceDbContext>();
    }
}
