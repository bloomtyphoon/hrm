using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.Modules.Organization.Application.DTOs;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Queries.GetPositionsByCompany;

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
        // Employee accounts can only query positions of their own company
        var accountType = _executionContext.GetClaimValue("AccountType");
        if (accountType == "Employee")
        {
            var companyIdClaim = _executionContext.GetClaimValue("CompanyId");
            if (!Guid.TryParse(companyIdClaim, out var employeeCompanyId))
                return Array.Empty<PositionDto>();

            if (request.CompanyId != employeeCompanyId)
                return Array.Empty<PositionDto>();
        }

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
}
