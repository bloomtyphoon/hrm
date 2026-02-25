using HRM.BuildingBlocks.Domain.Entities;

namespace HRM.BuildingBlocks.Infrastructure.Inbox;

/// <summary>
/// Inbox message entity for idempotent integration event processing.
/// Implements the Inbox pattern to guarantee exactly-once processing.
///
/// Each module has its own InboxMessages table (schema-separated).
/// Before an integration event handler processes an event, it checks
/// if an InboxMessage with the same EventId and HandlerName already exists.
/// If so, the event is skipped (already processed).
/// After successful processing, an InboxMessage is created atomically
/// with the handler's changes.
/// </summary>
public sealed class InboxMessage : Entity
{
    /// <summary>
    /// The integration event ID (from IIntegrationEvent.Id).
    /// </summary>
    public Guid EventId { get; private set; }

    /// <summary>
    /// The full type name of the integration event.
    /// </summary>
    public string EventType { get; private set; } = string.Empty;

    /// <summary>
    /// The handler that processed this event.
    /// Allows multiple handlers to independently track processing of the same event.
    /// </summary>
    public string HandlerName { get; private set; } = string.Empty;

    /// <summary>
    /// When the event was processed.
    /// </summary>
    public DateTime ProcessedOnUtc { get; private set; }

    private InboxMessage() { }

    /// <summary>
    /// Create a new inbox message to record that an event has been processed.
    /// </summary>
    public static InboxMessage Create(Guid eventId, string eventType, string handlerName)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type is required", nameof(eventType));
        if (string.IsNullOrWhiteSpace(handlerName))
            throw new ArgumentException("Handler name is required", nameof(handlerName));

        return new InboxMessage
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            EventType = eventType,
            HandlerName = handlerName,
            ProcessedOnUtc = DateTime.UtcNow
        };
    }
}
