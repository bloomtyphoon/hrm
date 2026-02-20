using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Identity.Domain.Events;

public sealed record SystemProfileCreatedDomainEvent(
    Guid ProfileId,
    Guid AccountId,
    bool IsSuperAdmin
) : DomainEvent;
