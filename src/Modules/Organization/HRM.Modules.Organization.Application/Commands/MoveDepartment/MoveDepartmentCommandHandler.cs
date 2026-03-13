using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Organization.Application.Security;
using HRM.Modules.Organization.Domain.Errors;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Commands.MoveDepartment;

internal sealed class MoveDepartmentCommandHandler : ICommandHandler<MoveDepartmentCommand>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;

    public MoveDepartmentCommandHandler(
        IDepartmentRepository departmentRepository,
        IDataScopeService dataScopeService,
        IExecutionContext executionContext)
    {
        _departmentRepository = departmentRepository;
        _dataScopeService = dataScopeService;
        _executionContext = executionContext;
    }

    public async Task<Result> Handle(MoveDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(request.DepartmentId, cancellationToken);
        if (department is null)
        {
            return Result.Failure(DepartmentErrors.NotFound(request.DepartmentId));
        }

        var rule = await _dataScopeService.GetCompanyScopeRuleAsync(
            _executionContext.UserId, OrganizationPermissions.Department.Update, cancellationToken);

        if (!CanAccessCompany(department.CompanyId, rule))
        {
            return Result.Failure(new ForbiddenError(
                "Department.AccessDenied", "You do not have permission to move this department."));
        }

        if (request.NewParentDepartmentId.HasValue)
        {
            var newParent = await _departmentRepository.GetByIdAsync(request.NewParentDepartmentId.Value, cancellationToken);
            if (newParent is null)
            {
                return Result.Failure(DepartmentErrors.ParentNotFound(request.NewParentDepartmentId.Value));
            }

            if (newParent.CompanyId != department.CompanyId)
            {
                return Result.Failure(DepartmentErrors.ParentInDifferentCompany(newParent.Id, department.CompanyId));
            }

            department.MoveTo(newParent);
        }
        else
        {
            department.MoveTo(null);
        }

        _departmentRepository.Update(department);

        return Result.Success();
    }

    private static bool CanAccessCompany(Guid companyId, DataScopeRule rule) => rule.Level switch
    {
        DataScopeLevel.Global => true,
        DataScopeLevel.Company => rule.DimensionIds.Contains(companyId),
        _ => false
    };
}
