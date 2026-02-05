namespace HRM.BuildingBlocks.Domain.Abstractions.Security;

/// <summary>
/// Data scope levels for filtering.
///
/// Two categories:
/// 1. Dimension-based: Company, Department, Position (static org structure)
/// 2. Set-based: Self, EmployeeSet (dynamic, resolved at runtime)
///
/// Ordering (narrowest to widest):
///   None(0) &lt; Self(1) &lt; EmployeeSet(2) &lt; Position(3) &lt; Department(4) &lt; Company(5) &lt; Global(6)
///
/// IMPORTANT:
/// - Dimension-based levels filter by property with [ScopeDimension] attribute
/// - Set-based levels filter by OwnerId IN (resolved IDs)
/// - EmployeeSet is for hierarchical scope (manager → subordinates)
/// </summary>
public enum DataScopeLevel
{
    /// <summary>
    /// No access — explicit deny. Rule denies all data.
    /// </summary>
    None = 0,

    /// <summary>
    /// Self scope — user sees only their own data.
    /// Filter: OwnerId == SelfEmployeeId
    /// </summary>
    Self = 1,

    /// <summary>
    /// Employee set scope — user sees data for a custom resolved set of employees.
    /// Used for hierarchical scope (manager sees subordinates' data).
    ///
    /// Filter: OwnerId IN (EmployeeIds)
    ///
    /// The Organization module resolves the employee set via IHierarchyScopeResolver.
    /// BB does NOT know how the set is resolved (graph traversal, etc.)
    /// </summary>
    EmployeeSet = 2,

    /// <summary>
    /// Position scope — user sees data for specific positions.
    /// Filter: [ScopeDimension(Position)] property IN (DimensionIds)
    /// </summary>
    Position = 3,

    /// <summary>
    /// Department scope — user sees data for specific departments.
    /// Filter: [ScopeDimension(Department)] property IN (DimensionIds)
    /// </summary>
    Department = 4,

    /// <summary>
    /// Company scope — user sees data for specific companies.
    /// Filter: [ScopeDimension(Company)] property IN (DimensionIds)
    /// </summary>
    Company = 5,

    /// <summary>
    /// Global scope — no filtering. User sees all data.
    /// Typically for super admin or system accounts.
    /// </summary>
    Global = 6
}
