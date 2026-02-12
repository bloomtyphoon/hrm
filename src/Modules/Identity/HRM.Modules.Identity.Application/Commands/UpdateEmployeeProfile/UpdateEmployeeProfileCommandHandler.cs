using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Application.Abstractions.Authorization;
using HRM.Modules.Identity.Domain.Errors;
using HRM.Modules.Identity.Domain.Repositories;

namespace HRM.Modules.Identity.Application.Commands.UpdateEmployeeProfile;

internal sealed class UpdateEmployeeProfileCommandHandler : ICommandHandler<UpdateEmployeeProfileCommand>
{
    private readonly IEmployeeProfileRepository _employeeProfileRepository;
    private readonly IAccountVisibilityFilter _visibilityFilter;

    public UpdateEmployeeProfileCommandHandler(
        IEmployeeProfileRepository employeeProfileRepository,
        IAccountVisibilityFilter visibilityFilter)
    {
        _employeeProfileRepository = employeeProfileRepository;
        _visibilityFilter = visibilityFilter;
    }

    public async Task<Result> Handle(UpdateEmployeeProfileCommand request, CancellationToken cancellationToken)
    {
        // Visibility check
        var visibleAccountIds = await _visibilityFilter.GetVisibleAccountIdsAsync(cancellationToken);
        if (visibleAccountIds != null && !visibleAccountIds.Contains(request.AccountId))
        {
            return Result.Failure(AccountErrors.NotFound(request.AccountId));
        }

        var profile = await _employeeProfileRepository.GetByAccountIdAsync(request.AccountId, cancellationToken);

        if (profile is null)
        {
            return Result.Failure(ProfileErrors.EmployeeProfileNotFound(request.AccountId));
        }

        profile.UpdatePrimaryAssignments(
            request.PrimaryCompanyId,
            request.PrimaryDepartmentId,
            request.PrimaryPositionId);

        profile.UpdateDefaultScopeLevel(request.DefaultScopeLevel);
        profile.SetCanAccessAllAssignedCompanies(request.CanAccessAllAssignedCompanies);

        return Result.Success();
    }
}
