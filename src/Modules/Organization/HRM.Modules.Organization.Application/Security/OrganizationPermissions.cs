using HRM.BuildingBlocks.Domain.Abstractions.Security;

namespace HRM.Modules.Organization.Application.Security;

/// <summary>
/// Strongly-typed permission descriptors for Organization module.
///
/// Each descriptor encodes Module + Entity + Action as a typed object,
/// eliminating magic strings from application code.
///
/// These descriptors:
/// - Are used by query/command handlers when calling IDataScopeService
/// - Are used by IPermissionService.HasPermissionAsync (typed overload)
/// - Produce .Name strings that match PermissionCatalog.xml and RouteSecurityMap.xml
///
/// The RoutePermissionMiddleware also parses RouteSecurityMap.xml permission strings
/// into PermissionDescriptor at runtime — so the middleware and handlers always
/// operate on the same typed model.
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
