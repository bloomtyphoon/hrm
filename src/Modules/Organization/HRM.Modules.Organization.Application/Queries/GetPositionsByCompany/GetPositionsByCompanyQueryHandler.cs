using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.Modules.Organization.Application.DTOs;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Queries.GetPositionsByCompany;

/// <summary>
/// Handler for GetPositionsByCompanyQuery.
///
/// Access rules:
/// - System: can query positions of any company.
/// - Employee: can only query positions of their own assigned company (from JWT CompanyId claim).
/// </summary>
internal sealed class GetPositionsByCompanyQueryHandler
    : IQueryHandler<GetPositionsByCompanyQuery, IReadOnlyList<PositionDto>>
{
    private readonly IPositionRepository _positionRepository;
    private readonly IExecutionContext _executionContext;

    public GetPositionsByCompanyQueryHandler(
        IPositionRepository positionRepository,
        IExecutionContext executionContext)
    {
        _positionRepository = positionRepository;
        _executionContext = executionContext;
    }

    public async Task<IReadOnlyList<PositionDto>> Handle(
        GetPositionsByCompanyQuery request,
        CancellationToken cancellationToken)
    {
        // Security boundary: Employee can only access their own company's positions
        if (!CanAccessCompany(request.CompanyId))
            return Array.Empty<PositionDto>();

        var positions = await _positionRepository.GetByCompanyIdAsync(request.CompanyId, cancellationToken);

        return positions.Select(p => new PositionDto(
            Id: p.Id,
            Code: p.Code,
            Title: p.Title,
            Description: p.Description,
            CompanyId: p.CompanyId,
            DepartmentId: p.DepartmentId,
            PositionLevel: p.PositionLevel,
            IsManagement: p.IsManagement,
            MaxHeadcount: p.MaxHeadcount,
            Status: p.Status.ToString(),
            CreatedAtUtc: p.CreatedAtUtc,
            ModifiedAtUtc: p.ModifiedAtUtc
        )).ToList();
    }

    private bool IsEmployeeAccount() =>
        _executionContext.GetClaimValue("AccountType") == "Employee";

    private Guid? GetEmployeeCompanyId()
    {
        var claim = _executionContext.GetClaimValue("CompanyId");
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    /// <summary>
    /// System accounts can access any company.
    /// Employee accounts can only access their assigned company.
    /// </summary>
    private bool CanAccessCompany(Guid companyId)
    {
        if (!IsEmployeeAccount()) return true;
        return GetEmployeeCompanyId() == companyId;
    }
}
