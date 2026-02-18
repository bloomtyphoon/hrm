using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Organization.Application.Commands.ActivateDepartment;

public sealed record ActivateDepartmentCommand(Guid DepartmentId) : IModuleCommand
{
    public string ModuleName => "Organization";
}
