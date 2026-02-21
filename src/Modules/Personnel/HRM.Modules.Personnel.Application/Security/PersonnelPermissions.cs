using HRM.BuildingBlocks.Domain.Abstractions.Security;

namespace HRM.Modules.Personnel.Application.Security;

/// <summary>
/// Strongly-typed permission descriptors for Personnel module.
///
/// Must stay in sync with:
///   - Resources/PermissionCatalog.xml (permission metadata and scopes)
///   - (Infrastructure) Security/RouteSecurityMap.xml (route-to-permission mappings)
///
/// Usage:
/// <code>
/// var rule = await _dataScopeService.GetScopeRuleAsync(
///     userId, PersonnelPermissions.Employee.View, ct);
/// </code>
/// </summary>
public static class PersonnelPermissions
{
    public const string Module = "Personnel";

    public static class Employee
    {
        public static readonly PermissionDescriptor View =
            new(Module, "Employee", "View");

        public static readonly PermissionDescriptor Create =
            new(Module, "Employee", "Create");

        public static readonly PermissionDescriptor Update =
            new(Module, "Employee", "Update");

        public static readonly PermissionDescriptor Terminate =
            new(Module, "Employee", "Terminate");
    }

    public static class Assignment
    {
        public static readonly PermissionDescriptor View =
            new(Module, "Assignment", "View");

        public static readonly PermissionDescriptor Create =
            new(Module, "Assignment", "Create");

        public static readonly PermissionDescriptor Update =
            new(Module, "Assignment", "Update");
    }
}
