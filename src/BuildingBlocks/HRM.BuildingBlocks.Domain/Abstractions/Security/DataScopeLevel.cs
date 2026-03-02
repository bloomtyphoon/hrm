namespace HRM.BuildingBlocks.Domain.Abstractions.Security;

/// <summary>
/// Data scope levels for filtering.
///
/// Two categories:
/// 1. Set-based: Self, DirectReports, EmployeeSet (dynamic, resolved at runtime)
/// 2. Dimension-based: Position, Department, Company, Country, Region (static attributes)
///
/// Ordering (narrowest to widest):
///   None(0) &lt; Self(1) &lt; DirectReports(2) &lt; EmployeeSet(3)
///        &lt; Position(4) &lt; Department(5) &lt; Company(6)
///        &lt; Country(7) &lt; Region(8) &lt; Global(9)
///
/// IMPORTANT:
/// - Dimension-based levels filter by property with [ScopeDimension] attribute
/// - Set-based levels filter by OwnerId IN (resolved IDs)
/// - DirectReports = depth-1 hierarchy (direct reports only, no recursion)
/// - EmployeeSet = full hierarchy (manager → all recursive subordinates)
/// </summary>
public enum DataScopeLevel
{
    /// <summary>No access — explicit deny.</summary>
    None = 0,

    /// <summary>
    /// Self scope — user sees only their own data.
    /// Filter: OwnerId == SelfEmployeeId
    /// </summary>
    Self = 1,

    /// <summary>
    /// Direct-reports scope — user sees their own data plus immediate subordinates.
    /// Equivalent to HierarchyScope(depth = 1).
    /// Filter: OwnerId IN (self + direct report IDs)
    /// </summary>
    DirectReports = 2,

    /// <summary>
    /// Full-hierarchy scope — user sees data for all recursive subordinates.
    /// Equivalent to HierarchyScope(depth = ∞).
    /// Filter: OwnerId IN (self + all recursive subordinate IDs)
    /// Backed by closure table; falls back to recursive CTE if not populated.
    /// </summary>
    EmployeeSet = 3,

    /// <summary>
    /// Position scope — user sees data for specific positions.
    /// Filter: [ScopeDimension(Position)] property IN (DimensionIds)
    /// </summary>
    Position = 4,

    /// <summary>
    /// Department scope — user sees data for specific departments.
    /// Filter: [ScopeDimension(Department)] property IN (DimensionIds)
    /// </summary>
    Department = 5,

    /// <summary>
    /// Company scope — user sees data for specific companies.
    /// Filter: [ScopeDimension(Company)] property IN (DimensionIds)
    /// </summary>
    Company = 6,

    /// <summary>
    /// Country scope — user sees data for specific countries.
    /// Filter: [ScopeDimension(Country)] property IN (DimensionIds)
    /// </summary>
    Country = 7,

    /// <summary>
    /// Region scope — user sees data across a geographic region (multiple countries).
    /// Filter: [ScopeDimension(Region)] property IN (DimensionIds)
    /// </summary>
    Region = 8,

    /// <summary>
    /// Global scope — no filtering. User sees all data within their tenant.
    /// Typically for platform admin or system accounts (which bypass tenant filter too).
    /// </summary>
    Global = 9
}
