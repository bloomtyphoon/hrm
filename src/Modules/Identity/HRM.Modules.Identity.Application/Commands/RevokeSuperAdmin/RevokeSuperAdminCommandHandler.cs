using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Domain.Errors;
using HRM.Modules.Identity.Domain.Repositories;

namespace HRM.Modules.Identity.Application.Commands.RevokeSuperAdmin;

internal sealed class RevokeSuperAdminCommandHandler : ICommandHandler<RevokeSuperAdminCommand>
{
    private readonly ISystemProfileRepository _systemProfileRepository;

    public RevokeSuperAdminCommandHandler(ISystemProfileRepository systemProfileRepository)
    {
        _systemProfileRepository = systemProfileRepository;
    }

    public async Task<Result> Handle(RevokeSuperAdminCommand request, CancellationToken cancellationToken)
    {
        var profile = await _systemProfileRepository.GetByAccountIdAsync(request.AccountId, cancellationToken);

        if (profile is null)
        {
            return Result.Failure(ProfileErrors.SystemProfileNotFound(request.AccountId));
        }

        if (!profile.IsSuperAdmin)
        {
            return Result.Failure(ProfileErrors.NotSuperAdmin);
        }

        profile.RevokeSuperAdmin();

        return Result.Success();
    }
}
