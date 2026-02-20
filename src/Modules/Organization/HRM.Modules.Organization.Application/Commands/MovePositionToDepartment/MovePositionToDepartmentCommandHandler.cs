using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Organization.Domain.Errors;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Commands.MovePositionToDepartment;

internal sealed class MovePositionToDepartmentCommandHandler : ICommandHandler<MovePositionToDepartmentCommand>
{
    private readonly IPositionRepository _positionRepository;
    private readonly IDepartmentRepository _departmentRepository;

    public MovePositionToDepartmentCommandHandler(
        IPositionRepository positionRepository,
        IDepartmentRepository departmentRepository)
    {
        _positionRepository = positionRepository;
        _departmentRepository = departmentRepository;
    }

    public async Task<Result> Handle(MovePositionToDepartmentCommand request, CancellationToken cancellationToken)
    {
        var position = await _positionRepository.GetByIdAsync(request.PositionId, cancellationToken);
        if (position is null)
        {
            return Result.Failure(PositionErrors.NotFound(request.PositionId));
        }

        if (request.DepartmentId.HasValue)
        {
            var department = await _departmentRepository.GetByIdAsync(request.DepartmentId.Value, cancellationToken);
            if (department is null)
            {
                return Result.Failure(PositionErrors.DepartmentNotFound(request.DepartmentId.Value));
            }

            if (department.CompanyId != position.CompanyId)
            {
                return Result.Failure(PositionErrors.DepartmentInDifferentCompany(department.Id, position.CompanyId));
            }
        }

        position.MoveToDepartment(request.DepartmentId);
        _positionRepository.Update(position);

        return Result.Success();
    }
}
