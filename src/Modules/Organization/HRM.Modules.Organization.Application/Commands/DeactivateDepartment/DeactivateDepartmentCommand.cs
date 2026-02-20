using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Organization.Application.Commands.DeactivateDepartment;

public sealed record DeactivateDepartmentCommand(Guid DepartmentId) : IModuleCommand
{
    public string ModuleName => "Organization";
}
