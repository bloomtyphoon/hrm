using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Domain.Abstractions.Results;

namespace HRM.Modules.Identity.Application.Queries.GetSystemProfile;

/// <summary>
/// Query to retrieve a system profile by account ID.
/// </summary>
public sealed record GetSystemProfileQuery(Guid AccountId) : IQuery<Result<SystemProfileDto>>;

public sealed record SystemProfileDto
{
    public Guid Id { get; init; }
    public Guid AccountId { get; init; }
    public bool IsSuperAdmin { get; init; }
    public string? Department { get; init; }
    public string? JobTitle { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? ModifiedAtUtc { get; init; }
}
