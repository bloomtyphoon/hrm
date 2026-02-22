using HRM.BuildingBlocks.Application.Abstractions.EventBus;
using HRM.Modules.Personnel.Domain.Events;
using HRM.Modules.Personnel.IntegrationEvents;
using MediatR;

namespace HRM.Modules.Personnel.Infrastructure.DomainEventHandlers;

internal sealed class EmployeeAssignmentsChangedDomainEventHandler
    : INotificationHandler<EmployeeAssignmentsChangedDomainEvent>
{
    private readonly IEventBus _eventBus;

    public EmployeeAssignmentsChangedDomainEventHandler(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task Handle(EmployeeAssignmentsChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        await _eventBus.PublishAsync(
            new EmployeeAssignmentsChangedIntegrationEvent(
                Id: Guid.NewGuid(),
                OccurredOnUtc: notification.OccurredOnUtc,
                EmployeeId: notification.EmployeeId,
                ActiveCompanyIds: notification.ActiveCompanyIds),
            cancellationToken);
    }
}
