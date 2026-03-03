using HRM.Modules.Attendance.Domain.Events;
using HRM.Modules.Attendance.Infrastructure.Persistence;
using HRM.Modules.Attendance.IntegrationEvents;
using MediatR;

namespace HRM.Modules.Attendance.Infrastructure.DomainEventHandlers;

/// <summary>
/// Handles AttendanceCheckedInDomainEvent by creating an integration event in the outbox.
/// The outbox ensures the integration event is published reliably (at-least-once) via AttendanceOutboxProcessor.
/// </summary>
internal sealed class AttendanceCheckedInDomainEventHandler
    : INotificationHandler<AttendanceCheckedInDomainEvent>
{
    private readonly AttendanceDbContext _dbContext;

    public AttendanceCheckedInDomainEventHandler(AttendanceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task Handle(AttendanceCheckedInDomainEvent notification, CancellationToken cancellationToken)
    {
        _dbContext.AddIntegrationEvent(new AttendanceCheckedInIntegrationEvent(
            Id: Guid.NewGuid(),
            OccurredOnUtc: notification.OccurredOnUtc,
            TenantId: notification.TenantId,
            EmployeeId: notification.EmployeeId,
            RecordId: notification.RecordId,
            Date: notification.Date,
            CheckInTimeUtc: notification.CheckInTimeUtc));

        return Task.CompletedTask;
    }
}
