using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Identity.Domain.Events;

public sealed record SystemProfileUpdatedDomainEvent(
    Guid ProfileId,
    Guid AccountId,
    string? Department,
    string? JobTitle
) : DomainEvent;
