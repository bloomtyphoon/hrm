namespace HRM.BuildingBlocks.Application.Abstractions.Organization;

/// <summary>
/// Cross-module query interface for Organization data.
///
/// DESIGN: This interface lives in BuildingBlocks as a CONTRACT.
/// - Organization module IMPLEMENTS this interface
/// - Personnel module (and others) CONSUME this interface
/// - No direct dependency between modules
///
/// Usage scenarios:
/// - Personnel needs to validate CompanyId exists before creating assignment
/// - Personnel needs to display Company/Department/Position names
/// - Payroll needs to get company settings
///
/// Note: Returns DTOs, not domain entities (to avoid cross-module domain leakage).
/// </summary>
public interface IOrganizationQuery
{
    #region Company Queries

    /// <summary>
    /// Check if company exists.
    /// </summary>
    Task<bool> CompanyExistsAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get company info by ID.
    /// </summary>
    Task<OrganizationItemDto?> GetCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get multiple companies by IDs.
    /// </summary>
    Task<IReadOnlyList<OrganizationItemDto>> GetCompaniesAsync(
        IEnumerable<Guid> companyIds,
        CancellationToken cancellationToken = default);

    #endregion

    #region Department Queries

    /// <summary>
    /// Check if department exists.
    /// </summary>
    Task<bool> DepartmentExistsAsync(Guid departmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get department info by ID.
    /// </summary>
    Task<OrganizationItemDto?> GetDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get multiple departments by IDs.
    /// </summary>
    Task<IReadOnlyList<OrganizationItemDto>> GetDepartmentsAsync(
        IEnumerable<Guid> departmentIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all departments in a company.
    /// </summary>
    Task<IReadOnlyList<OrganizationItemDto>> GetDepartmentsByCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Position Queries

    /// <summary>
    /// Check if position exists.
    /// </summary>
    Task<bool> PositionExistsAsync(Guid positionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get position info by ID.
    /// </summary>
    Task<OrganizationItemDto?> GetPositionAsync(Guid positionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get multiple positions by IDs.
    /// </summary>
    Task<IReadOnlyList<OrganizationItemDto>> GetPositionsAsync(
        IEnumerable<Guid> positionIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all positions in a department.
    /// </summary>
    Task<IReadOnlyList<OrganizationItemDto>> GetPositionsByDepartmentAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Validation

    /// <summary>
    /// Validate that company, department, and position exist and are related.
    /// Returns true if all exist and department belongs to company, position belongs to department.
    /// </summary>
    Task<OrganizationValidationResult> ValidateAssignmentAsync(
        Guid companyId,
        Guid departmentId,
        Guid positionId,
        CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// DTO for organization items (Company, Department, Position).
/// Used for cross-module queries to avoid domain entity leakage.
/// </summary>
public sealed record OrganizationItemDto
{
    /// <summary>
    /// Entity ID.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Entity code.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Entity name/title.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Parent ID (e.g., CompanyId for Department).
    /// </summary>
    public Guid? ParentId { get; init; }

    /// <summary>
    /// Whether the entity is active.
    /// </summary>
    public bool IsActive { get; init; } = true;
}

/// <summary>
/// Result of organization assignment validation.
/// </summary>
public sealed record OrganizationValidationResult
{
    /// <summary>
    /// Whether the validation passed.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Error message if validation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Company info (if exists).
    /// </summary>
    public OrganizationItemDto? Company { get; init; }

    /// <summary>
    /// Department info (if exists).
    /// </summary>
    public OrganizationItemDto? Department { get; init; }

    /// <summary>
    /// Position info (if exists).
    /// </summary>
    public OrganizationItemDto? Position { get; init; }

    /// <summary>
    /// Create a successful validation result.
    /// </summary>
    public static OrganizationValidationResult Success(
        OrganizationItemDto company,
        OrganizationItemDto department,
        OrganizationItemDto position) => new()
    {
        IsValid = true,
        Company = company,
        Department = department,
        Position = position
    };

    /// <summary>
    /// Create a failed validation result.
    /// </summary>
    public static OrganizationValidationResult Failure(string errorMessage) => new()
    {
        IsValid = false,
        ErrorMessage = errorMessage
    };
}
