using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Domain.Errors;
using HRM.Modules.Identity.Domain.Repositories;

namespace HRM.Modules.Identity.Application.Commands.UpdateEmployeeProfile;

internal sealed class UpdateEmployeeProfileCommandHandler : ICommandHandler<UpdateEmployeeProfileCommand>
{
    private readonly IEmployeeProfileRepository _employeeProfileRepository;

    public UpdateEmployeeProfileCommandHandler(IEmployeeProfileRepository employeeProfileRepository)
    {
        _employeeProfileRepository = employeeProfileRepository;
    }

    public async Task<Result> Handle(UpdateEmployeeProfileCommand request, CancellationToken cancellationToken)
    {
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
