using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Personnel.Domain.Events;

/// <summary>
/// Raised when a new employee is created in the Personnel module.
/// Consumed by the domain event handler to publish EmployeeCreatedIntegrationEvent
/// for cross-module communication (e.g., Identity module creates Account + EmployeeProfile).
/// </summary>
public sealed record EmployeeCreatedDomainEvent(
    Guid TenantId,
    Guid EmployeeId,
    string EmployeeCode,
    string FirstName,
    string LastName,
    string Email,
    string? Phone
) : DomainEvent;
