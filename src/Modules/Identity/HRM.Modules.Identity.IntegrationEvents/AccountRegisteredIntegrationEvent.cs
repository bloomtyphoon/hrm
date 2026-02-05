using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Identity.IntegrationEvents;

/// <summary>
/// Integration event raised when an account is registered.
/// Published asynchronously via Transactional Outbox pattern.
/// </summary>
public sealed record AccountRegisteredIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid AccountId,
    string Username,
    string Email,
    string FullName
) : IIntegrationEvent;
