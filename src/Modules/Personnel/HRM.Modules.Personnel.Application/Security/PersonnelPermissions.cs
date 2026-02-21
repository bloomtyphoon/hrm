using HRM.BuildingBlocks.Domain.Abstractions.Security;

namespace HRM.Modules.Personnel.Application.Security;

/// <summary>
/// Strongly-typed permission descriptors for Personnel module.
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
