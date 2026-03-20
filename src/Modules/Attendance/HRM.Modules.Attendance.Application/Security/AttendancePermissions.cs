using HRM.BuildingBlocks.Domain.Abstractions.Security;

namespace HRM.Modules.Attendance.Application.Security;

/// <summary>
/// Strongly-typed permission descriptors for Attendance module.
/// Each descriptor encodes Module + Entity + Action as a typed object.
///
/// These descriptors:
/// - Are used by query/command handlers when calling IDataScopeService
/// - Produce .Name strings that match PermissionCatalog.xml and RouteSecurityMap.xml
/// </summary>
public static class AttendancePermissions
{
    public const string Module = "Attendance";

    public static class Record
    {
        public static readonly PermissionDescriptor CheckIn =
            new(Module, "Record", "CheckIn");

        public static readonly PermissionDescriptor CheckOut =
            new(Module, "Record", "CheckOut");

        public static readonly PermissionDescriptor View =
            new(Module, "Record", "View");

        public static readonly PermissionDescriptor ManualRecord =
            new(Module, "Record", "ManualRecord");

        public static readonly PermissionDescriptor Update =
            new(Module, "Record", "Update");

        public static readonly PermissionDescriptor Delete =
            new(Module, "Record", "Delete");
    }

    public static class Shift
    {
        public static readonly PermissionDescriptor View =
            new(Module, "Shift", "View");

        public static readonly PermissionDescriptor Manage =
            new(Module, "Shift", "Manage");

        public static readonly PermissionDescriptor Assign =
            new(Module, "Shift", "Assign");
    }

    public static class Leave
    {
        public static readonly PermissionDescriptor ViewTypes =
            new(Module, "Leave", "ViewTypes");

        public static readonly PermissionDescriptor ManageTypes =
            new(Module, "Leave", "ManageTypes");

        public static readonly PermissionDescriptor Request =
            new(Module, "Leave", "Request");

        public static readonly PermissionDescriptor ViewRequests =
            new(Module, "Leave", "ViewRequests");

        public static readonly PermissionDescriptor Approve =
            new(Module, "Leave", "Approve");

        public static readonly PermissionDescriptor ManageSettings =
            new(Module, "Leave", "ManageSettings");
    }
}
