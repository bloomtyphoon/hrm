using HRM.BuildingBlocks.Domain.Abstractions.Events;

namespace HRM.Modules.Personnel.Domain.Events;

/// <summary>
/// Raised when a new employee is created in the Personnel module.
/// Consumed by the domain event handler to publish EmployeeCreatedIntegrationEvent
/// for cross-module communication (e.g., Identity module creates Account + EmployeeProfile).
/// Also consumed by EmployeeCreatedHierarchyHandler to initialize the closure table row.
/// </summary>
public sealed record EmployeeCreatedDomainEvent(
    Guid TenantId,
    Guid EmployeeId,
    string EmployeeCode,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    Guid? ManagerId = null
) : DomainEvent;
