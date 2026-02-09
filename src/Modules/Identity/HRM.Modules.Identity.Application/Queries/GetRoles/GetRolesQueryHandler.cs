using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Application.Pagination;
using HRM.Modules.Identity.Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Identity.Application.Queries.GetRoles;

public sealed class GetRolesQueryHandler
    : IQueryHandler<GetRolesQuery, PagedResult<RoleSummaryDto>>
{
    private readonly IIdentityQueryContext _context;

    public GetRolesQueryHandler(IIdentityQueryContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<RoleSummaryDto>> Handle(
        GetRolesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Roles.AsNoTracking();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(r =>
                r.Name.ToLower().Contains(searchTerm) ||
                (r.Description != null && r.Description.ToLower().Contains(searchTerm)));
        }

        // Apply system role filter
        if (request.IsSystemRole.HasValue)
        {
            query = query.Where(r => r.IsSystemRole == request.IsSystemRole.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(r => r.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new RoleSummaryDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsSystemRole = r.IsSystemRole,
                PermissionCount = r.Permissions.Count,
                CreatedAtUtc = r.CreatedAtUtc,
                ModifiedAtUtc = r.ModifiedAtUtc
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<RoleSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
