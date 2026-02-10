using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Domain.Errors;
using HRM.Modules.Identity.Domain.Repositories;

namespace HRM.Modules.Identity.Application.Commands.UpdateSystemProfile;

internal sealed class UpdateSystemProfileCommandHandler : ICommandHandler<UpdateSystemProfileCommand>
{
    private readonly ISystemProfileRepository _systemProfileRepository;

    public UpdateSystemProfileCommandHandler(ISystemProfileRepository systemProfileRepository)
    {
        _systemProfileRepository = systemProfileRepository;
    }

    public async Task<Result> Handle(UpdateSystemProfileCommand request, CancellationToken cancellationToken)
    {
        var profile = await _systemProfileRepository.GetByAccountIdAsync(request.AccountId, cancellationToken);

        if (profile is null)
        {
            return Result.Failure(ProfileErrors.SystemProfileNotFound(request.AccountId));
        }

        profile.Update(request.Department, request.JobTitle, request.Notes);

        return Result.Success();
    }
}
