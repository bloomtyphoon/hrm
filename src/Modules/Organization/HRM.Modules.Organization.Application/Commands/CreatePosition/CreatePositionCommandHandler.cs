using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Organization.Domain.Entities;
using HRM.Modules.Organization.Domain.Errors;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Commands.CreatePosition;

internal sealed class CreatePositionCommandHandler : ICommandHandler<CreatePositionCommand, Guid>
{
    private readonly IPositionRepository _positionRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IDepartmentRepository _departmentRepository;

    public CreatePositionCommandHandler(
        IPositionRepository positionRepository,
        ICompanyRepository companyRepository,
        IDepartmentRepository departmentRepository)
    {
        _positionRepository = positionRepository;
        _companyRepository = companyRepository;
        _departmentRepository = departmentRepository;
    }

    public async Task<Result<Guid>> Handle(CreatePositionCommand request, CancellationToken cancellationToken)
    {
        // 1. Verify company exists
        var company = await _companyRepository.GetByIdAsync(request.CompanyId, cancellationToken);
        if (company is null)
        {
            return Result.Failure<Guid>(PositionErrors.CompanyNotFound(request.CompanyId));
        }

        // 2. Verify department exists and belongs to company (if specified)
        if (request.DepartmentId.HasValue)
        {
            var department = await _departmentRepository.GetByIdAsync(request.DepartmentId.Value, cancellationToken);
            if (department is null)
            {
                return Result.Failure<Guid>(PositionErrors.DepartmentNotFound(request.DepartmentId.Value));
            }

            if (department.CompanyId != request.CompanyId)
            {
                return Result.Failure<Guid>(PositionErrors.DepartmentInDifferentCompany(department.Id, request.CompanyId));
            }
        }

        // 3. Check code uniqueness within company
        if (await _positionRepository.ExistsByCodeInCompanyAsync(request.CompanyId, request.Code, cancellationToken))
        {
            return Result.Failure<Guid>(PositionErrors.CodeAlreadyExists(request.Code, request.CompanyId));
        }

        // 4. Create Position aggregate
        var position = Position.Create(
            companyId: request.CompanyId,
            code: request.Code,
            title: request.Title,
            positionLevel: request.PositionLevel,
            isManagement: request.IsManagement,
            departmentId: request.DepartmentId,
            description: request.Description,
            maxHeadcount: request.MaxHeadcount
        );

        // 5. Add to repository
        _positionRepository.Add(position);

        return Result.Success(position.Id);
    }
}
