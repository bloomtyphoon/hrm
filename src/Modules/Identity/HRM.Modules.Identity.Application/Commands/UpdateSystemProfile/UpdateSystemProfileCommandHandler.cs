using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Domain.Errors;
using HRM.Modules.Identity.Domain.Repositories;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.Modules.Identity.Application.Abstractions.Authentication;
using HRM.Modules.Identity.Application.Abstractions.Data;
using HRM.Modules.Identity.Application.Security;

namespace HRM.Modules.Identity.Application.Commands.UpdateSystemProfile;

internal sealed class UpdateSystemProfileCommandHandler : ICommandHandler<UpdateSystemProfileCommand>
{
    private readonly ISystemProfileRepository _systemProfileRepository;
    private readonly IDataScopeService _dataScopeService;
    private readonly ICurrentUserService _currentUser;
    private readonly IIdentityQueryContext _queryContext;

    public UpdateSystemProfileCommandHandler(
        ISystemProfileRepository systemProfileRepository,
        IDataScopeService dataScopeService,
        ICurrentUserService currentUser,
        IIdentityQueryContext queryContext)
    {
        _systemProfileRepository = systemProfileRepository;
        _dataScopeService = dataScopeService;
        _currentUser = currentUser;
        _queryContext = queryContext;
    }

    public async Task<Result> Handle(UpdateSystemProfileCommand request, CancellationToken cancellationToken)
    {
        var rule = await _dataScopeService.GetScopeRuleAsync(
            _currentUser.UserId, IdentityPermissions.Account.View, cancellationToken);
        if (!await AccountScopeFilter.IsAccessibleAsync(rule, _currentUser.UserId, request.AccountId, _queryContext, cancellationToken))
        {
            return Result.Failure(AccountErrors.NotFound(request.AccountId));
        }

        var profile = await _systemProfileRepository.GetByAccountIdAsync(request.AccountId, cancellationToken);

        if (profile is null)
        {
            return Result.Failure(ProfileErrors.SystemProfileNotFound(request.AccountId));
        }

        profile.Update(request.Department, request.JobTitle, request.Notes);

        return Result.Success();
    }
}
