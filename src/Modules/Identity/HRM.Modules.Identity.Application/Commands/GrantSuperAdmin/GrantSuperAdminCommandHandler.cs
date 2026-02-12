using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Domain.Errors;
using HRM.Modules.Identity.Domain.Repositories;

namespace HRM.Modules.Identity.Application.Commands.GrantSuperAdmin;

internal sealed class GrantSuperAdminCommandHandler : ICommandHandler<GrantSuperAdminCommand>
{
    private readonly ISystemProfileRepository _systemProfileRepository;

    public GrantSuperAdminCommandHandler(ISystemProfileRepository systemProfileRepository)
    {
        _systemProfileRepository = systemProfileRepository;
    }

    public async Task<Result> Handle(GrantSuperAdminCommand request, CancellationToken cancellationToken)
    {
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
