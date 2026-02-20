using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.Modules.Personnel.Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Personnel.Application.Queries.GetEmployeeById;

/// <summary>
/// Handler for GetEmployeeByIdQuery.
/// Returns full employee detail or null if not found.
/// </summary>
public sealed class GetEmployeeByIdQueryHandler
    : IQueryHandler<GetEmployeeByIdQuery, EmployeeDetailDto?>
{
    private readonly IPersonnelQueryContext _context;

    public GetEmployeeByIdQueryHandler(IPersonnelQueryContext context)
    {
        _context = context;
    }

    public async Task<EmployeeDetailDto?> Handle(
        GetEmployeeByIdQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Employees
            .AsNoTracking()
            .Where(e => e.Id == request.EmployeeId)
            .Select(e => new EmployeeDetailDto
            {
                Id = e.Id,
                EmployeeCode = e.EmployeeCode,
                FirstName = e.FirstName,
                LastName = e.LastName,
                FullName = e.FirstName + " " + e.LastName,
                Email = e.Email,
                Phone = e.Phone,
                DateOfBirth = e.DateOfBirth,
                HireDate = e.HireDate,
                TerminationDate = e.TerminationDate,
                Status = e.Status,
                ManagerId = e.ManagerId,
                PrimaryCompanyId = e.PrimaryCompanyId,
                PrimaryDepartmentId = e.PrimaryDepartmentId,
                PrimaryPositionId = e.PrimaryPositionId,
                CreatedAtUtc = e.CreatedAtUtc,
                ModifiedAtUtc = e.ModifiedAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
