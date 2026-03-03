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
    }
}
