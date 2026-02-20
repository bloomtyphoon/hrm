using HRM.BuildingBlocks.Domain.Abstractions.Events;
using HRM.Modules.Identity.Domain.Enums;

namespace HRM.Modules.Identity.Domain.Events;

public sealed record AccountCreatedDomainEvent(
    Guid AccountId,
    string Username,
    string Email,
    AccountType AccountType
) : DomainEvent;
