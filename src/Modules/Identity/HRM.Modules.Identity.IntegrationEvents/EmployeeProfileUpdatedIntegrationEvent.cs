using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Identity.IntegrationEvents;

public sealed record EmployeeProfileUpdatedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid ProfileId,
    Guid AccountId,
    Guid EmployeeId
) : IIntegrationEvent;
