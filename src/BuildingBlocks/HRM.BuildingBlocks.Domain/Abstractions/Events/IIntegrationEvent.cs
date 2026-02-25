namespace HRM.BuildingBlocks.Domain.Abstractions.Events;

/// <summary>
/// Interface for integration events (cross-module communication).
/// Decoupled from MediatR — integration events are stored in the Outbox table
/// and dispatched by the OutboxProcessor background service, which resolves
/// IIntegrationEventHandler&lt;T&gt; directly from the DI container.
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>
    /// Unique identifier of the integration event.
    /// Used for idempotency checks in handlers.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// When the integration event occurred (UTC).
    /// Represents when the original domain event happened, not when it was published.
    /// </summary>
    DateTime OccurredOnUtc { get; }
}
