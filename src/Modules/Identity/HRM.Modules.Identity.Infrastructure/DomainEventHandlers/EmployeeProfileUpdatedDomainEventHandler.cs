using HRM.Modules.Identity.Domain.Events;
using HRM.Modules.Identity.Infrastructure.Persistence;
using HRM.Modules.Identity.IntegrationEvents;
using MediatR;

namespace HRM.Modules.Identity.Infrastructure.DomainEventHandlers;

internal sealed class EmployeeProfileUpdatedDomainEventHandler
    : INotificationHandler<EmployeeProfileUpdatedDomainEvent>
{
    private readonly IdentityDbContext _dbContext;

    public EmployeeProfileUpdatedDomainEventHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task Handle(EmployeeProfileUpdatedDomainEvent notification, CancellationToken cancellationToken)
    {
        _dbContext.AddIntegrationEvent(
            new EmployeeProfileUpdatedIntegrationEvent(
                Id: Guid.NewGuid(),
                OccurredOnUtc: notification.OccurredOnUtc,
                ProfileId: notification.ProfileId,
                AccountId: notification.AccountId,
                EmployeeId: notification.EmployeeId));

        return Task.CompletedTask;
    }
}
