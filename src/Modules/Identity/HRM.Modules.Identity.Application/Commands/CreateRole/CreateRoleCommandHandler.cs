using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Domain.Entities;
using HRM.Modules.Identity.Domain.Errors;
using HRM.Modules.Identity.Domain.Repositories;
using HRM.Modules.Identity.Domain.ValueObjects;

namespace HRM.Modules.Identity.Application.Commands.CreateRole;

internal sealed class CreateRoleCommandHandler : ICommandHandler<CreateRoleCommand, Guid>
{
    private readonly IRoleRepository _roleRepository;

    public CreateRoleCommandHandler(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<Result<Guid>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        // 1. Check name uniqueness within same company scope
        if (await _roleRepository.ExistsByNameAsync(request.Name, request.CompanyId, cancellationToken))
        {
            return Result.Failure<Guid>(RoleErrors.NameAlreadyExists(request.Name));
        }

        // 2. Create Role aggregate
        var role = Role.Create(request.Name, request.Description, request.IsSystemRole, request.CompanyId);

        // 3. Add permissions
        var permissions = request.Permissions
            .Select(p => RolePermission.Create(p.Module, p.Entity, p.Action, p.Scope))
            .ToList();

        role.AddPermissions(permissions);

        // 4. Finalize creation (raises domain event)
        role.FinalizeCreation();

        // 5. Persist
        _roleRepository.Add(role);

        return Result.Success(role.Id);
    }
}
