using HRM.BuildingBlocks.Domain.Abstractions.Results;

namespace HRM.Modules.Organization.Domain.Errors;

/// <summary>
/// Static error definitions for Tenant operations.
/// </summary>
public static class TenantErrors
{
    public static ConflictError CodeAlreadyExists(string code) =>
        new("Tenant.CodeAlreadyExists",
            $"Tenant code '{code}' is already in use. Please choose a different code.");

    public static NotFoundError NotFound(Guid id) =>
        new("Tenant.NotFound",
            $"Tenant with ID '{id}' was not found.");

    public static ConflictError AlreadyActive(string code) =>
        new("Tenant.AlreadyActive",
            $"Tenant '{code}' is already active.");

    public static ConflictError AlreadySuspended(string code) =>
        new("Tenant.AlreadySuspended",
            $"Tenant '{code}' is already suspended.");

    public static ConflictError AlreadyDeactivated(string code) =>
        new("Tenant.AlreadyDeactivated",
            $"Tenant '{code}' is already deactivated.");

    public static ValidationError SystemTenantImmutable() =>
        new("Tenant.SystemTenantImmutable",
            "The system tenant cannot be modified, suspended, or deactivated.");
}
