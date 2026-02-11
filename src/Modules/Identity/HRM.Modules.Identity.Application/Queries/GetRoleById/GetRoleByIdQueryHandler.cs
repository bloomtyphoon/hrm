using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Domain.Errors;
using HRM.Modules.Identity.Domain.Repositories;

namespace HRM.Modules.Identity.Application.Queries.GetRoleById;

public sealed class GetRoleByIdQueryHandler
    : IQueryHandler<GetRoleByIdQuery, Result<RoleDetailDto>>
{
    private readonly IRoleRepository _roleRepository;

    public GetRoleByIdQueryHandler(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<Result<RoleDetailDto>> Handle(
        GetRoleByIdQuery request,
        CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);

        if (role is null)
        {
            return Result.Failure<RoleDetailDto>(RoleErrors.NotFound(request.RoleId));
        }

        var dto = new RoleDetailDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsSystemRole = role.IsSystemRole,
            CompanyId = role.CompanyId,
            PermissionCount = role.PermissionCount,
            Permissions = role.Permissions.Select(p => new RolePermissionDto
            {
                Module = p.Module,
                Entity = p.Entity,
                Action = p.Action,
                Scope = p.Scope,
                PermissionKey = p.PermissionKey
            }).ToList(),
            CreatedAtUtc = role.CreatedAtUtc,
            ModifiedAtUtc = role.ModifiedAtUtc
        };

        return Result.Success(dto);
    }
}
