using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Domain.Errors;
using HRM.Modules.Identity.Domain.Repositories;
using HRM.Modules.Identity.Domain.ValueObjects;

namespace HRM.Modules.Identity.Application.Commands.UpdateRole;

internal sealed class UpdateRoleCommandHandler : ICommandHandler<UpdateRoleCommand>
{
    private readonly IRoleRepository _roleRepository;

    public UpdateRoleCommandHandler(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<Result> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        // 1. Get role
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure(RoleErrors.NotFound(request.RoleId));
        }

        // 2. Check name uniqueness (exclude current role)
        if (await _roleRepository.ExistsByNameAsync(request.Name, request.RoleId, cancellationToken))
        {
            return Result.Failure(RoleErrors.NameAlreadyExists(request.Name));
        }

        // 3. Update name and description
        role.Update(request.Name, request.Description);

        // 4. Replace permissions
        var permissions = request.Permissions
            .Select(p => RolePermission.Create(p.Module, p.Entity, p.Action, p.Scope))
            .ToList();

        role.SetPermissions(permissions);

        return Result.Success();
    }
}
