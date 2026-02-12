using HRM.BuildingBlocks.Domain.Abstractions.Results;

namespace HRM.Modules.Identity.Domain.Errors;

/// <summary>
/// Static error definitions for Profile operations (SystemProfile, EmployeeProfile).
/// </summary>
public static class ProfileErrors
{
    public static NotFoundError SystemProfileNotFound(Guid accountId) =>
        new("Profile.SystemProfileNotFound",
            $"System profile for account '{accountId}' was not found.");

    public static NotFoundError EmployeeProfileNotFound(Guid accountId) =>
        new("Profile.EmployeeProfileNotFound",
            $"Employee profile for account '{accountId}' was not found.");

    public static ConflictError SystemProfileAlreadyExists(Guid accountId) =>
        new("Profile.SystemProfileAlreadyExists",
            $"System profile for account '{accountId}' already exists.");

    public static ConflictError EmployeeProfileAlreadyExists(Guid accountId) =>
        new("Profile.EmployeeProfileAlreadyExists",
            $"Employee profile for account '{accountId}' already exists.");

    public static readonly ValidationError AccountNotSystemType =
        new("Profile.AccountNotSystemType",
            "Cannot create a system profile for a non-system account.");

    public static readonly ValidationError AccountNotEmployeeType =
        new("Profile.AccountNotEmployeeType",
            "Cannot create an employee profile for a non-employee account.");

    public static readonly ConflictError AlreadySuperAdmin =
        new("Profile.AlreadySuperAdmin",
            "This account already has super admin privileges.");

    public static readonly ConflictError NotSuperAdmin =
        new("Profile.NotSuperAdmin",
            "This account does not have super admin privileges.");
}
