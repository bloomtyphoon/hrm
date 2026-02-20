using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Organization.Application.Commands.RemoveDepartmentManager;

public sealed record RemoveDepartmentManagerCommand(Guid DepartmentId) : IModuleCommand
{
    public string ModuleName => "Organization";
}
