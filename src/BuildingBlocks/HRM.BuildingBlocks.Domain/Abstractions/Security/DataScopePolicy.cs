namespace HRM.BuildingBlocks.Domain.Abstractions.Security;

/// <summary>
/// Combines multiple DataScopeRules with logical operators.
///
/// Use cases:
/// 1. User has multiple roles with different scopes (OR combination)
///    - HR Dept A: Department [D1]
///    - Talent Reviewer: Position [P9]
///    → WHERE DepartmentId IN (D1) OR PositionId IN (P9)
///
/// 2. Hierarchical + dimension constraint (AND combination)
///    - Manager sees subordinates AND only in their department
///    → WHERE OwnerId IN (subordinates) AND DepartmentId IN (D1)
///
/// Combination logic:
/// - Different roles (expand access) → OR
/// - Same role constraints (restrict access) → AND
///
/// Usage:
/// <code>
/// // Multiple roles - OR
/// var policy = DataScopePolicy.Or(
///     DataScopeRule.Department([D1]),
///     DataScopeRule.Position([P9])
/// );
///
/// // With constraint - AND
/// var policy = DataScopePolicy.And(
///     DataScopeRule.EmployeeSet(subordinateIds),
///     DataScopeRule.Department([D1])
/// );
///
/// // Single rule (backward compatible)
/// var policy = DataScopePolicy.Single(DataScopeRule.Company([C1]));
/// </code>
/// </summary>
public sealed class DataScopePolicy
{
    /// <summary>
    /// The rules to combine.
    /// </summary>
    public IReadOnlyList<DataScopeRule> Rules { get; }

    /// <summary>
    /// How to combine the rules.
    /// </summary>
    public PolicyCombinator Combinator { get; }

    /// <summary>
    /// Whether this policy grants any access.
    /// For OR: any rule has access
    /// For AND: all rules have access
    /// </summary>
    public bool HasAccess => Combinator switch
    {
        PolicyCombinator.Or => Rules.Any(r => r.HasAccess),
        PolicyCombinator.And => Rules.All(r => r.HasAccess),
        _ => false
    };

    /// <summary>
    /// Whether this is a single-rule policy (no combination needed).
    /// </summary>
    public bool IsSingleRule => Rules.Count == 1;

    /// <summary>
    /// Get the single rule if this is a single-rule policy.
    /// </summary>
    public DataScopeRule? SingleRule => IsSingleRule ? Rules[0] : null;

    /// <summary>
    /// Whether any rule grants global access.
    /// If true, the entire policy can be simplified to Global.
    /// </summary>
    public bool HasGlobalAccess => Rules.Any(r => r.Level == DataScopeLevel.Global);

    private DataScopePolicy(IEnumerable<DataScopeRule> rules, PolicyCombinator combinator)
    {
        var ruleList = rules.ToList();

        if (ruleList.Count == 0)
            throw new ArgumentException("Policy must have at least one rule.", nameof(rules));

        Rules = ruleList;
        Combinator = combinator;
    }

    #region Factory Methods

    /// <summary>
    /// Create a policy that ORs multiple rules (expands access).
    /// Use when combining grants from different roles.
    /// </summary>
    public static DataScopePolicy Or(params DataScopeRule[] rules)
        => new(rules, PolicyCombinator.Or);

    /// <summary>
    /// Create a policy that ORs multiple rules (expands access).
    /// </summary>
    public static DataScopePolicy Or(IEnumerable<DataScopeRule> rules)
        => new(rules, PolicyCombinator.Or);

    /// <summary>
    /// Create a policy that ANDs multiple rules (restricts access).
    /// Use when applying constraints within same context.
    /// </summary>
    public static DataScopePolicy And(params DataScopeRule[] rules)
        => new(rules, PolicyCombinator.And);

    /// <summary>
    /// Create a policy that ANDs multiple rules (restricts access).
    /// </summary>
    public static DataScopePolicy And(IEnumerable<DataScopeRule> rules)
        => new(rules, PolicyCombinator.And);

    /// <summary>
    /// Create a single-rule policy (backward compatible).
    /// </summary>
    public static DataScopePolicy Single(DataScopeRule rule)
        => new([rule], PolicyCombinator.Or);

    /// <summary>
    /// Create a policy that denies all access.
    /// </summary>
    public static DataScopePolicy None()
        => Single(DataScopeRule.None());

    /// <summary>
    /// Create a policy that grants global access.
    /// </summary>
    public static DataScopePolicy Global()
        => Single(DataScopeRule.Global());

    #endregion

    #region Optimization

    /// <summary>
    /// Simplify the policy by removing redundant rules.
    ///
    /// Optimizations:
    /// - If any rule is Global (OR) → return Global
    /// - If any rule is None (AND) → return None
    /// - Remove None rules from OR
    /// - Remove Global rules from AND
    /// - Merge same-level dimension rules
    /// </summary>
    public DataScopePolicy Simplify()
    {
        if (Rules.Count <= 1)
            return this;

        // OR with Global = Global
        if (Combinator == PolicyCombinator.Or && HasGlobalAccess)
            return Global();

        // AND with None = None
        if (Combinator == PolicyCombinator.And && Rules.Any(r => r.Level == DataScopeLevel.None))
            return None();

        var simplified = Rules
            .Where(r => Combinator == PolicyCombinator.Or
                ? r.Level != DataScopeLevel.None  // Remove None from OR
                : r.Level != DataScopeLevel.Global) // Remove Global from AND
            .ToList();

        if (simplified.Count == 0)
            return Combinator == PolicyCombinator.Or ? None() : Global();

        if (simplified.Count == 1)
            return Single(simplified[0]);

        // TODO: Merge same-level dimension rules
        // e.g., Department([D1]) OR Department([D2]) → Department([D1, D2])

        return new DataScopePolicy(simplified, Combinator);
    }

    #endregion
}

/// <summary>
/// How to combine rules in a policy.
/// </summary>
public enum PolicyCombinator
{
    /// <summary>
    /// OR combination — any rule grants access.
    /// Use for combining grants from different roles.
    /// </summary>
    Or,

    /// <summary>
    /// AND combination — all rules must grant access.
    /// Use for applying constraints within same context.
    /// </summary>
    And
}
