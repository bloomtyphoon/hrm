using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Application.Abstractions.Data;
using HRM.Modules.Identity.Domain.Errors;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Identity.Application.Queries.GetEmployeeProfile;

internal sealed class GetEmployeeProfileQueryHandler
    : IQueryHandler<GetEmployeeProfileQuery, Result<EmployeeProfileDto>>
{
    private readonly IIdentityQueryContext _context;

    public GetEmployeeProfileQueryHandler(IIdentityQueryContext context)
    {
        _context = context;
    }

    public async Task<Result<EmployeeProfileDto>> Handle(
        GetEmployeeProfileQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await _context.EmployeeProfiles
            .AsNoTracking()
            .Where(ep => ep.AccountId == request.AccountId)
            .Select(ep => new EmployeeProfileDto
            {
                Id = ep.Id,
                AccountId = ep.AccountId,
                EmployeeId = ep.EmployeeId,
                DefaultScopeLevel = ep.DefaultScopeLevel,
                PrimaryCompanyId = ep.PrimaryCompanyId,
                PrimaryDepartmentId = ep.PrimaryDepartmentId,
                PrimaryPositionId = ep.PrimaryPositionId,
                CanAccessAllAssignedCompanies = ep.CanAccessAllAssignedCompanies,
                CreatedAtUtc = ep.CreatedAtUtc,
                ModifiedAtUtc = ep.ModifiedAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (profile is null)
        {
            return Result.Failure<EmployeeProfileDto>(ProfileErrors.EmployeeProfileNotFound(request.AccountId));
        }

        return Result.Success(profile);
    }
}
