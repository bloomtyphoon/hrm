using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Organization.Application.Security;
using HRM.Modules.Organization.Domain.Entities;
using HRM.Modules.Organization.Domain.Errors;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Commands.CreateDepartment;

internal sealed class CreateDepartmentCommandHandler : ICommandHandler<CreateDepartmentCommand, Guid>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;

    public CreateDepartmentCommandHandler(
        IDepartmentRepository departmentRepository,
        ICompanyRepository companyRepository,
        IDataScopeService dataScopeService,
        IExecutionContext executionContext)
    {
        _departmentRepository = departmentRepository;
        _companyRepository = companyRepository;
        _dataScopeService = dataScopeService;
        _executionContext = executionContext;
    }

    public async Task<Result<Guid>> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        // Scope check
        var rule = await _dataScopeService.GetCompanyScopeRuleAsync(
            _executionContext.UserId, OrganizationPermissions.Department.Create, cancellationToken);

        if (!CanAccessCompany(request.CompanyId, rule))
        {
            return Result.Failure<Guid>(new ForbiddenError(
                "Department.AccessDenied", "You do not have permission to create departments in this company."));
        }

        // 1. Verify company exists
        var company = await _companyRepository.GetByIdAsync(request.CompanyId, cancellationToken);
        if (company is null)
        {
            return Result.Failure<Guid>(DepartmentErrors.CompanyNotFound(request.CompanyId));
        }

        // 2. Check code uniqueness within company
        if (await _departmentRepository.ExistsByCodeInCompanyAsync(request.CompanyId, request.Code, cancellationToken))
        {
            return Result.Failure<Guid>(DepartmentErrors.CodeAlreadyExists(request.Code, request.CompanyId));
        }

        // 3. Handle parent department (if specified)
        Department department;
        if (request.ParentDepartmentId.HasValue)
        {
            var parent = await _departmentRepository.GetByIdAsync(request.ParentDepartmentId.Value, cancellationToken);
            if (parent is null)
            {
                return Result.Failure<Guid>(DepartmentErrors.ParentNotFound(request.ParentDepartmentId.Value));
            }

            if (parent.CompanyId != request.CompanyId)
            {
                return Result.Failure<Guid>(DepartmentErrors.ParentInDifferentCompany(parent.Id, request.CompanyId));
            }

            department = Department.CreateChild(parent, request.Code, request.Name, request.ManagerId);
        }
        else
        {
            department = Department.CreateRoot(company.TenantId, request.CompanyId, request.Code, request.Name, request.ManagerId);
        }

        // 4. Add to repository
        _departmentRepository.Add(department);

        return Result.Success(department.Id);
    }

    private static bool CanAccessCompany(Guid companyId, DataScopeRule rule) => rule.Level switch
    {
        DataScopeLevel.Global => true,
        DataScopeLevel.Company => rule.DimensionIds.Contains(companyId),
        _ => false
    };
}
