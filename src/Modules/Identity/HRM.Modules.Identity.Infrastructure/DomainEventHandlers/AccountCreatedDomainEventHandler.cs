using HRM.Modules.Identity.Domain.Events;
using HRM.Modules.Identity.Infrastructure.Persistence;
using HRM.Modules.Identity.IntegrationEvents;
using MediatR;

namespace HRM.Modules.Identity.Infrastructure.DomainEventHandlers;

/// <summary>
/// Domain event handler for AccountCreatedDomainEvent.
/// Creates integration event for cross-module communication.
/// </summary>
internal sealed class AccountCreatedDomainEventHandler
    : INotificationHandler<AccountCreatedDomainEvent>
{
    private readonly IdentityDbContext _dbContext;

    public AccountCreatedDomainEventHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task Handle(AccountCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        var integrationEvent = new AccountRegisteredIntegrationEvent(
            Id: Guid.NewGuid(),
            OccurredOnUtc: DateTime.UtcNow,
            AccountId: notification.AccountId,
            Username: notification.Username,
            Email: notification.Email,
            FullName: string.Empty
        );

        _dbContext.AddIntegrationEvent(integrationEvent);

        return Task.CompletedTask;
    }
}
