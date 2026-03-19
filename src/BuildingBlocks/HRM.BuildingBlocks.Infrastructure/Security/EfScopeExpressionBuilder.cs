using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using HRM.BuildingBlocks.Domain.Abstractions.Security;

namespace HRM.BuildingBlocks.Infrastructure.Security;

/// <summary>
/// Translates DataScopeRule/DataScopePolicy to EF Core Expressions.
///
/// DESIGN PRINCIPLES:
/// - BB does NOT know about org structure (Company, Department, Position)
/// - Uses [ScopeDimension("key")] attribute to discover dimension properties
/// - Caches property selectors at startup for runtime performance
/// - Category-based dispatch: no switch on specific DataScopeLevel values
/// - Supports dynamic scope levels loaded from DB
/// </summary>
public static class EfScopeExpressionBuilder
{
    // Cache: EntityType → (DimensionKey → PropertyInfo)
    private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> _dimensionCache = new();

    #region Public API

    /// <summary>Build expression from a single rule.</summary>
    public static Expression<Func<T, bool>> Build<T>(DataScopeRule rule)
        where T : class, IScopedEntity
    {
        return BuildRuleExpression<T>(rule);
    }

    /// <summary>Build expression from a policy (multiple rules combined).</summary>
    public static Expression<Func<T, bool>> Build<T>(DataScopePolicy policy)
        where T : class, IScopedEntity
    {
        var simplified = policy.Simplify();

        if (simplified.IsSingleRule)
            return BuildRuleExpression<T>(simplified.SingleRule!);

        var parameter = Expression.Parameter(typeof(T), "x");
        Expression? combined = null;

        foreach (var rule in simplified.Rules)
        {
            var ruleExpr = BuildRuleExpression<T>(rule);
            var body = ReplacementVisitor.Replace(
                ruleExpr.Body, ruleExpr.Parameters[0], parameter);

            combined = combined is null
                ? body
                : simplified.Combinator == PolicyCombinator.Or
                    ? Expression.OrElse(combined, body)
                    : Expression.AndAlso(combined, body);
        }

        return Expression.Lambda<Func<T, bool>>(combined!, parameter);
    }

    /// <summary>Build expression for owned entities (Self scope only).</summary>
    public static Expression<Func<T, bool>> BuildOwnerScope<T>(DataScopeRule rule)
        where T : class, IOwnedEntity
    {
        return rule.Level.Category switch
        {
            ScopeCategory.Global => _ => true,
            ScopeCategory.None => _ => false,
            ScopeCategory.Set when rule.Level == DataScopeLevel.Self && rule.SelfEmployeeId.HasValue =>
                x => x.OwnerId == rule.SelfEmployeeId.Value,
            ScopeCategory.Set when rule.EmployeeIds.Count > 0 =>
                BuildOwnerContains<T>(rule.EmployeeIds),
            ScopeCategory.Dimension => _ => true, // Wider scope = all access for owned entities
            _ => _ => false
        };
    }

    /// <summary>
    /// Pre-warm the cache for an entity type.
    /// Call at startup for known entity types to avoid first-request latency.
    /// </summary>
    public static void WarmCache<T>() where T : class, IScopedEntity
    {
        GetDimensionProperties(typeof(T));
    }

    #endregion

    #region Private Implementation

    private static Expression<Func<T, bool>> BuildRuleExpression<T>(DataScopeRule rule)
        where T : class, IScopedEntity
    {
        return rule.Level.Category switch
        {
            ScopeCategory.Global => _ => true,
            ScopeCategory.None => _ => false,

            // Set-based scopes (filter by OwnerId IN ids)
            ScopeCategory.Set when rule.Level == DataScopeLevel.Self && rule.SelfEmployeeId.HasValue =>
                x => x.OwnerId == rule.SelfEmployeeId.Value,
            ScopeCategory.Set when rule.EmployeeIds.Count > 0 =>
                BuildOwnerContains<T>(rule.EmployeeIds),

            // Dimension-based scopes (filter by [ScopeDimension] property IN ids)
            ScopeCategory.Dimension when rule.Level.DimensionKey is not null =>
                BuildDimensionContains<T>(rule.Level.DimensionKey, rule.DimensionIds),

            _ => _ => false
        };
    }

    private static Expression<Func<T, bool>> BuildOwnerContains<T>(IReadOnlyCollection<Guid> employeeIds)
        where T : class, IOwnedEntity
    {
        var idList = employeeIds.ToList();
        return x => idList.Contains(x.OwnerId);
    }

    private static Expression<Func<T, bool>> BuildDimensionContains<T>(
        string dimensionKey,
        IReadOnlyCollection<Guid> dimensionIds)
        where T : class, IScopedEntity
    {
        var properties = GetDimensionProperties(typeof(T));

        if (!properties.TryGetValue(dimensionKey, out var property))
        {
            // Entity doesn't have this dimension - deny access
            return _ => false;
        }

        var parameter = Expression.Parameter(typeof(T), "x");
        var propertyAccess = Expression.Property(parameter, property);

        // Handle nullable Guid
        if (property.PropertyType == typeof(Guid?))
        {
            return BuildNullableContains<T>(parameter, propertyAccess, dimensionIds);
        }

        // Non-nullable Guid
        var idList = dimensionIds.ToList();
        var containsMethod = typeof(List<Guid>).GetMethod("Contains", [typeof(Guid)])!;
        var idsConstant = Expression.Constant(idList);
        var contains = Expression.Call(idsConstant, containsMethod, propertyAccess);

        return Expression.Lambda<Func<T, bool>>(contains, parameter);
    }

    private static Expression<Func<T, bool>> BuildNullableContains<T>(
        ParameterExpression parameter,
        MemberExpression propertyAccess,
        IReadOnlyCollection<Guid> dimensionIds)
        where T : class
    {
        var idList = dimensionIds.ToList();

        // x.Property != null && idList.Contains(x.Property.Value)
        var notNull = Expression.NotEqual(propertyAccess, Expression.Constant(null, typeof(Guid?)));
        var getValue = Expression.Property(propertyAccess, "Value");

        var containsMethod = typeof(List<Guid>).GetMethod("Contains", [typeof(Guid)])!;
        var idsConstant = Expression.Constant(idList);
        var contains = Expression.Call(idsConstant, containsMethod, getValue);

        var combined = Expression.AndAlso(notNull, contains);

        return Expression.Lambda<Func<T, bool>>(combined, parameter);
    }

    /// <summary>
    /// Discover dimension properties using [ScopeDimension("key")] attribute.
    /// Returns: DimensionKey → PropertyInfo
    /// </summary>
    private static Dictionary<string, PropertyInfo> GetDimensionProperties(Type entityType)
    {
        return _dimensionCache.GetOrAdd(entityType, type =>
        {
            var result = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var attr = property.GetCustomAttribute<ScopeDimensionAttribute>();
                if (attr is not null)
                {
                    result[attr.DimensionKey] = property;
                }
            }

            return result;
        });
    }

    #endregion

    #region Helper Classes

    private sealed class ReplacementVisitor : ExpressionVisitor
    {
        private readonly Expression _oldValue;
        private readonly Expression _newValue;

        private ReplacementVisitor(Expression oldValue, Expression newValue)
        {
            _oldValue = oldValue;
            _newValue = newValue;
        }

        public static Expression Replace(Expression expression, Expression oldValue, Expression newValue)
        {
            return new ReplacementVisitor(oldValue, newValue).Visit(expression);
        }

        public override Expression? Visit(Expression? node)
        {
            if (node == _oldValue)
                return _newValue;
            return base.Visit(node);
        }
    }

    #endregion
}
