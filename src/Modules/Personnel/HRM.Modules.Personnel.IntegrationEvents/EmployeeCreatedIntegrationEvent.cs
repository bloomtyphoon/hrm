using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Personnel.IntegrationEvents;

/// <summary>
/// Published when a new employee is created in the Personnel module.
/// Consumed by Identity module to auto-create Account + EmployeeProfile.
/// </summary>
public sealed record EmployeeCreatedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    Guid EmployeeId,
    string EmployeeCode,
    string FirstName,
    string LastName,
    string Email,
    string? Phone
) : IIntegrationEvent;
