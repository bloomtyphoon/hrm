using HRM.BuildingBlocks.Domain.Abstractions.Security;

namespace HRM.Modules.Identity.Application.Security;

public static class IdentityPermissions
{
    public const string Module = "Identity";

    public static class Account
    {
        public static readonly PermissionDescriptor View          = new(Module, "Account", "View");
        public static readonly PermissionDescriptor Create        = new(Module, "Account", "Create");
        public static readonly PermissionDescriptor Update        = new(Module, "Account", "Update");
        public static readonly PermissionDescriptor Delete        = new(Module, "Account", "Delete");
        public static readonly PermissionDescriptor ResetPassword = new(Module, "Account", "ResetPassword");
        public static readonly PermissionDescriptor AssignRole    = new(Module, "Account", "AssignRole");
    }

    public static class Role
    {
        public static readonly PermissionDescriptor View             = new(Module, "Role", "View");
        public static readonly PermissionDescriptor Create           = new(Module, "Role", "Create");
        public static readonly PermissionDescriptor Update           = new(Module, "Role", "Update");
        public static readonly PermissionDescriptor Delete           = new(Module, "Role", "Delete");
        public static readonly PermissionDescriptor AssignPermission = new(Module, "Role", "AssignPermission");
    }

    public static class SystemProfile
    {
        public static readonly PermissionDescriptor View             = new(Module, "SystemProfile", "View");
        public static readonly PermissionDescriptor Create           = new(Module, "SystemProfile", "Create");
        public static readonly PermissionDescriptor Update           = new(Module, "SystemProfile", "Update");
        public static readonly PermissionDescriptor ManageSuperAdmin = new(Module, "SystemProfile", "ManageSuperAdmin");
    }

    public static class EmployeeProfile
    {
        public static readonly PermissionDescriptor View   = new(Module, "EmployeeProfile", "View");
        public static readonly PermissionDescriptor Create = new(Module, "EmployeeProfile", "Create");
        public static readonly PermissionDescriptor Update = new(Module, "EmployeeProfile", "Update");
    }
}
