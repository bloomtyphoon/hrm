using HRM.BuildingBlocks.Domain.Abstractions.Security;

namespace HRM.Modules.Organization.Application.Security;

/// <summary>
/// Strongly-typed permission descriptors for Organization module.
///
/// Must stay in sync with:
///   - Resources/PermissionCatalog.xml (permission metadata and scopes)
///   - (Infrastructure) Security/RouteSecurityMap.xml (route-to-permission mappings)
///
/// Usage:
/// <code>
/// var rule = await _dataScopeService.GetScopeRuleAsync(
///     userId, OrganizationPermissions.Company.View, ct);
/// </code>
/// </summary>
public static class OrganizationPermissions
{
    public const string Module = "Organization";

    public static class Company
    {
        public static readonly PermissionDescriptor View =
            new(Module, "Company", "View");

        public static readonly PermissionDescriptor Create =
            new(Module, "Company", "Create");

        public static readonly PermissionDescriptor Update =
            new(Module, "Company", "Update");
    }

    public static class Department
    {
        public static readonly PermissionDescriptor View =
            new(Module, "Department", "View");

        public static readonly PermissionDescriptor Create =
            new(Module, "Department", "Create");

        public static readonly PermissionDescriptor Update =
            new(Module, "Department", "Update");
    }

    public static class Position
    {
        public static readonly PermissionDescriptor View =
            new(Module, "Position", "View");

        public static readonly PermissionDescriptor Create =
            new(Module, "Position", "Create");

        public static readonly PermissionDescriptor Update =
            new(Module, "Position", "Update");
    }
}
