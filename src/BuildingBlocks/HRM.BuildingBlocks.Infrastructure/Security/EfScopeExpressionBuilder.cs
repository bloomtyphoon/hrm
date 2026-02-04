using System.Linq.Expressions;
using HRM.BuildingBlocks.Domain.Abstractions.Security;

namespace HRM.BuildingBlocks.Infrastructure.Security;

/// <summary>
/// Translates DataScopeRule to EF Core Expression.
///
/// IMPORTANT: This class contains NO business logic.
/// It only translates the level-based rule to Expression format.
/// All business logic is in IDataScopeService / IDataScopeRuleProvider.
///
/// Usage:
/// <code>
/// var rule = await dataScopeService.GetScopeRuleAsync(userId, permission);
/// var expression = EfScopeExpressionBuilder.Build&lt;Employee&gt;(rule);
/// var filtered = query.Where(expression);
/// </code>
/// </summary>
public static class EfScopeExpressionBuilder
{
    /// <summary>
    /// Build filter expression for IScopedEntity.
    /// Switches on rule.Level to select the correct dimension.
    /// </summary>
    public static Expression<Func<T, bool>> Build<T>(DataScopeRule rule)
        where T : class, IScopedEntity
    {
        return rule.Level switch
        {
            DataScopeLevel.Global => _ => true,

            DataScopeLevel.Company => BuildContains<T>(
                rule.DimensionIds, x => x.CompanyId),

            DataScopeLevel.Department => BuildContains<T>(
                rule.DimensionIds, x => x.DepartmentId),

            DataScopeLevel.Position => BuildContains<T>(
                rule.DimensionIds, x => x.PositionId),

            DataScopeLevel.Self => BuildSelfScope<T>(rule.SelfEmployeeId!.Value),

            // None or unknown → deny all
            _ => _ => false
        };
    }

    /// <summary>
    /// Build filter expression for company-scoped entities only.
    /// </summary>
    public static Expression<Func<T, bool>> BuildCompanyScope<T>(DataScopeRule rule)
        where T : class, ICompanyScopedEntity
    {
        return rule.Level switch
        {
            DataScopeLevel.Global => _ => true,
            DataScopeLevel.Company => BuildContains<T>(
                rule.DimensionIds, x => x.CompanyId),
            _ when rule.Level > DataScopeLevel.Company => _ => true,
            _ => _ => false
        };
    }

    /// <summary>
    /// Build filter expression for owned entities only.
    /// </summary>
    public static Expression<Func<T, bool>> BuildOwnerScope<T>(DataScopeRule rule)
        where T : class, IOwnedEntity
    {
        return rule.Level switch
        {
            DataScopeLevel.Global => _ => true,
            DataScopeLevel.Self when rule.SelfEmployeeId.HasValue =>
                BuildOwnerEquals<T>(rule.SelfEmployeeId.Value),
            _ when rule.Level > DataScopeLevel.Self => _ => true,
            _ => _ => false
        };
    }

    /// <summary>
    /// Build custom filter expression with entity-specific selectors.
    /// Use when entity doesn't implement IScopedEntity.
    /// </summary>
    public static Expression<Func<T, bool>> BuildCustom<T>(
        DataScopeRule rule,
        Expression<Func<T, Guid?>> companySelector,
        Expression<Func<T, Guid?>> departmentSelector,
        Expression<Func<T, Guid?>> positionSelector,
        Expression<Func<T, Guid>> ownerSelector)
        where T : class
    {
        var parameter = Expression.Parameter(typeof(T), "x");

        return rule.Level switch
        {
            DataScopeLevel.Global => _ => true,

            DataScopeLevel.Self when rule.SelfEmployeeId.HasValue =>
                BuildEqualsExpression(parameter, ownerSelector, rule.SelfEmployeeId.Value),

            DataScopeLevel.Position =>
                BuildContainsExpression(parameter, positionSelector, rule.DimensionIds),

            DataScopeLevel.Department =>
                BuildContainsExpression(parameter, departmentSelector, rule.DimensionIds),

            DataScopeLevel.Company =>
                BuildContainsExpression(parameter, companySelector, rule.DimensionIds),

            _ => _ => false
        };
    }

    private static Expression<Func<T, bool>> BuildSelfScope<T>(Guid employeeId)
        where T : class, IScopedEntity
    {
        return x => x.OwnerId == employeeId;
    }

    private static Expression<Func<T, bool>> BuildOwnerEquals<T>(Guid employeeId)
        where T : class, IOwnedEntity
    {
        return x => x.OwnerId == employeeId;
    }

    private static Expression<Func<T, bool>> BuildContains<T>(
        IReadOnlyCollection<Guid> ids,
        Expression<Func<T, Guid?>> selector)
        where T : class
    {
        var idList = ids;
        var parameter = Expression.Parameter(typeof(T), "x");
        var selectorBody = ReplacementVisitor.Replace(
            selector.Body, selector.Parameters[0], parameter);

        var notNull = Expression.NotEqual(selectorBody, Expression.Constant(null, typeof(Guid?)));
        var getValue = Expression.Property(selectorBody, "Value");

        var containsMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(Guid));

        var idsConstant = Expression.Constant(idList);
        var contains = Expression.Call(containsMethod, idsConstant, getValue);
        var combined = Expression.AndAlso(notNull, contains);

        return Expression.Lambda<Func<T, bool>>(combined, parameter);
    }

    private static Expression<Func<T, bool>> BuildEqualsExpression<T>(
        ParameterExpression parameter,
        Expression<Func<T, Guid>> selector,
        Guid value)
        where T : class
    {
        var body = ReplacementVisitor.Replace(
            selector.Body, selector.Parameters[0], parameter);
        var constant = Expression.Constant(value);
        var equals = Expression.Equal(body, constant);
        return Expression.Lambda<Func<T, bool>>(equals, parameter);
    }

    private static Expression<Func<T, bool>> BuildContainsExpression<T>(
        ParameterExpression parameter,
        Expression<Func<T, Guid?>> selector,
        IReadOnlyCollection<Guid> ids)
        where T : class
    {
        var selectorBody = ReplacementVisitor.Replace(
            selector.Body, selector.Parameters[0], parameter);

        var notNull = Expression.NotEqual(selectorBody, Expression.Constant(null, typeof(Guid?)));
        var getValue = Expression.Property(selectorBody, "Value");

        var containsMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(Guid));

        var idsConstant = Expression.Constant(ids);
        var contains = Expression.Call(containsMethod, idsConstant, getValue);
        var combined = Expression.AndAlso(notNull, contains);

        return Expression.Lambda<Func<T, bool>>(combined, parameter);
    }

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
}
