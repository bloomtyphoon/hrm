using HRM.BuildingBlocks.Domain.Entities;

namespace HRM.BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Outbox message entity for reliable event publishing.
/// Implements the Transactional Outbox pattern.
///
/// Lives in Infrastructure because it is a persistence concern,
/// not a domain concept. Domain events and integration events
/// are the domain abstractions; OutboxMessage is the mechanism
/// that guarantees their delivery.
/// </summary>
public sealed class OutboxMessage : AuditableEntity, IAggregateRoot
{
    public string Type { get; private set; } = string.Empty;

    public string Content { get; private set; } = string.Empty;

    public DateTime OccurredOnUtc { get; private set; }

    public DateTime? ProcessedOnUtc { get; private set; }

    public string? Error { get; private set; }

    public int AttemptCount { get; private set; }

    private OutboxMessage()
    {
    }

    public static OutboxMessage Create(
        string type,
        string content,
        DateTime occurredOnUtc)
    {
        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Type cannot be null or empty", nameof(type));

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be null or empty", nameof(content));

        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = type,
            Content = content,
            OccurredOnUtc = occurredOnUtc,
            ProcessedOnUtc = null,
            Error = null,
            AttemptCount = 0
        };
    }

    public void MarkAsProcessed()
    {
        ProcessedOnUtc = DateTime.UtcNow;
        Error = null;
    }

    public void MarkAsFailed(string error)
    {
        Error = error;
        AttemptCount++;
    }

    public bool CanRetry(int maxAttempts = 3)
    {
        return AttemptCount < maxAttempts && ProcessedOnUtc == null;
    }

    public bool IsProcessed() => ProcessedOnUtc.HasValue;

    public bool HasError() => !string.IsNullOrEmpty(Error);
}
