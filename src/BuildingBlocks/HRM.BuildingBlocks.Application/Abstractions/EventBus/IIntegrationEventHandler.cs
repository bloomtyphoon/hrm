using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.BuildingBlocks.Application.Abstractions.EventBus;

/// <summary>
/// Handler interface for processing integration events from other modules.
/// Resolved directly from the DI container by the OutboxProcessor — no MediatR involved.
///
/// Handlers must be registered explicitly in the module's DI configuration:
/// <code>
/// services.AddScoped&lt;IIntegrationEventHandler&lt;EmployeeCreatedIntegrationEvent&gt;,
///     EmployeeCreatedIntegrationEventHandler&gt;();
/// </code>
/// </summary>
/// <typeparam name="TIntegrationEvent">Type of integration event to handle</typeparam>
public interface IIntegrationEventHandler<in TIntegrationEvent>
    where TIntegrationEvent : IIntegrationEvent
{
    Task Handle(TIntegrationEvent notification, CancellationToken cancellationToken);
}
