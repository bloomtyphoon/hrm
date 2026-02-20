using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Application.Abstractions.Authentication;
using HRM.Modules.Identity.Application.Abstractions.Data;
using HRM.Modules.Identity.Domain.Errors;
using HRM.Modules.Identity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Identity.Application.Commands.DeleteRole;

internal sealed class DeleteRoleCommandHandler : ICommandHandler<DeleteRoleCommand>
{
    private readonly IRoleRepository _roleRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IIdentityQueryContext _context;

    public DeleteRoleCommandHandler(
        IRoleRepository roleRepository,
        ICurrentUserService currentUser,
        IIdentityQueryContext context)
    {
        _roleRepository = roleRepository;
        _currentUser = currentUser;
        _context = context;
    }

    public async Task<Result> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);

        if (role is null)
        {
            return Result.Failure(RoleErrors.NotFound(request.RoleId));
        }

        // Employee access check — same rules as GetRoleById
        if (_currentUser.IsEmployeeAccount())
        {
            // Employee cannot delete global roles
            if (!role.CompanyId.HasValue)
            {
                return Result.Failure(RoleErrors.NotFound(request.RoleId));
            }

            // Employee can only delete roles for their assigned companies
            var hasAccess = await _context.EmployeeProfiles
                .AsNoTracking()
                .Where(ep => ep.AccountId == _currentUser.UserId)
                .AnyAsync(ep => ep.CompanyAccess.Any(ca => ca.CompanyId == role.CompanyId.Value),
                    cancellationToken);

            if (!hasAccess)
            {
                return Result.Failure(RoleErrors.NotFound(request.RoleId));
            }
        }

        // Soft delete (raises RoleDeletedDomainEvent)
        role.Delete();

        return Result.Success();
    }
}
