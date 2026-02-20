using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Application.Abstractions.Authorization;
using HRM.Modules.Identity.Domain.Errors;
using HRM.Modules.Identity.Domain.Repositories;

namespace HRM.Modules.Identity.Application.Commands.GrantSuperAdmin;

internal sealed class GrantSuperAdminCommandHandler : ICommandHandler<GrantSuperAdminCommand>
{
    private readonly ISystemProfileRepository _systemProfileRepository;
    private readonly IAccountVisibilityFilter _visibilityFilter;

    public GrantSuperAdminCommandHandler(
        ISystemProfileRepository systemProfileRepository,
        IAccountVisibilityFilter visibilityFilter)
    {
        _systemProfileRepository = systemProfileRepository;
        _visibilityFilter = visibilityFilter;
    }

    public async Task<Result> Handle(GrantSuperAdminCommand request, CancellationToken cancellationToken)
    {
        var visibleAccountIds = await _visibilityFilter.GetVisibleAccountIdsAsync(cancellationToken);
        if (visibleAccountIds != null && !visibleAccountIds.Contains(request.AccountId))
        {
            return Result.Failure(AccountErrors.NotFound(request.AccountId));
        }

        var profile = await _systemProfileRepository.GetByAccountIdAsync(request.AccountId, cancellationToken);

        if (profile is null)
        {
            return Result.Failure(ProfileErrors.SystemProfileNotFound(request.AccountId));
        }

        if (profile.IsSuperAdmin)
        {
            return Result.Failure(ProfileErrors.AlreadySuperAdmin);
        }

        profile.GrantSuperAdmin();

        return Result.Success();
    }
}
