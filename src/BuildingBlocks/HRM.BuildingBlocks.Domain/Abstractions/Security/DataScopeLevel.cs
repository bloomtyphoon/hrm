namespace HRM.BuildingBlocks.Domain.Abstractions.Security;

/// <summary>
/// Hierarchy of data scope levels, from narrowest to widest.
/// Encodes the organizational dimension used for data filtering.
///
/// Ordering: None(0) &lt; Self(1) &lt; Position(2) &lt; Department(3) &lt; Company(4) &lt; Global(5)
///
/// The integer ordering enables semantic comparisons:
///   if (rule.Level >= DataScopeLevel.Department) — user sees department-level or wider
///
/// IMPORTANT: Comparison is for semantic checks only, NOT for filter building.
/// Filter building must switch on the exact Level to select the correct dimension.
/// </summary>
public enum DataScopeLevel
{
    /// <summary>
    /// No access — explicit deny. Rule denies all data.
    /// </summary>
    None = 0,

    /// <summary>
    /// Self scope — user sees only their own data.
    /// Filter dimension: EmployeeId (via SelfEmployeeId).
    /// </summary>
    Self = 1,

    /// <summary>
    /// Position scope — user sees data for specific positions.
    /// Filter dimension: PositionId (via DimensionIds).
    /// </summary>
    Position = 2,

    /// <summary>
    /// Department scope — user sees data for specific departments.
    /// Filter dimension: DepartmentId (via DimensionIds).
    /// </summary>
    Department = 3,

    /// <summary>
    /// Company scope — user sees data for specific companies.
    /// Filter dimension: CompanyId (via DimensionIds).
    /// </summary>
    Company = 4,

    /// <summary>
    /// Global scope — no filtering. User sees all data.
    /// Typically for super admin or system accounts.
    /// </summary>
    Global = 5
}
