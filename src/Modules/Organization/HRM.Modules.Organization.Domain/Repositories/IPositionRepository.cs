using HRM.Modules.Organization.Domain.Entities;

namespace HRM.Modules.Organization.Domain.Repositories;

/// <summary>
/// Repository interface for Position aggregate.
/// </summary>
public interface IPositionRepository
{
    Task<Position?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Position>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Position>> GetByDepartmentIdAsync(Guid departmentId, CancellationToken cancellationToken = default);

    Task<bool> ExistsByCodeInCompanyAsync(Guid companyId, string code, CancellationToken cancellationToken = default);

    void Add(Position position);

    void Update(Position position);

    void Remove(Position position);
}
