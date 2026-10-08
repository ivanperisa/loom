using System.Linq.Expressions;
using Loom.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Common.Querying;

/// <summary>
/// Declares, once per resource, which fields a list can be searched and sorted by and how rows are projected.
/// Everything is translated to SQL; unknown sort keys fall back to the default order.
/// </summary>
public sealed class ListSpec<TEntity, TResponse> where TEntity : EntityBase
{
    private readonly IReadOnlyList<Expression<Func<TEntity, string?>>> _searchFields;
    private readonly IReadOnlyDictionary<string, Func<IQueryable<TEntity>, bool, IOrderedQueryable<TEntity>>> _sorts;
    private readonly Func<IQueryable<TEntity>, bool, IOrderedQueryable<TEntity>> _defaultSort;

    internal ListSpec(
        IReadOnlyList<Expression<Func<TEntity, string?>>> searchFields,
        IReadOnlyDictionary<string, Func<IQueryable<TEntity>, bool, IOrderedQueryable<TEntity>>> sorts,
        Func<IQueryable<TEntity>, bool, IOrderedQueryable<TEntity>> defaultSort,
        Expression<Func<TEntity, TResponse>> projection)
    {
        _searchFields = searchFields;
        _sorts = sorts;
        _defaultSort = defaultSort;
        Projection = projection;
    }

    public Expression<Func<TEntity, TResponse>> Projection { get; }

    public IQueryable<TEntity> ApplySearch(IQueryable<TEntity> query, string? term)
    {
        if (term is null || _searchFields.Count == 0) return query;

        var pattern = $"%{EscapeLike(term.ToLower())}%";
        var row = Expression.Parameter(typeof(TEntity), "x");
        var like = typeof(DbFunctionsExtensions).GetMethod(nameof(DbFunctionsExtensions.Like),
            [typeof(DbFunctions), typeof(string), typeof(string), typeof(string)])!;
        var toLower = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;

        Expression? any = null;
        foreach (var field in _searchFields)
        {
            var value = new ReplaceParameter(field.Parameters[0], row).Visit(field.Body);
            var match = Expression.Call(like, Expression.Constant(EF.Functions), Expression.Call(value, toLower),
                Expression.Constant(pattern), Expression.Constant("\\"));
            any = any is null ? match : Expression.OrElse(any, match);
        }
        return query.Where(Expression.Lambda<Func<TEntity, bool>>(any!, row));
    }

    public IOrderedQueryable<TEntity> ApplySort(IQueryable<TEntity> query, ListQuery listQuery)
    {
        var (key, descending) = listQuery.ResolveSort();
        var orderBy = key is not null && _sorts.TryGetValue(key, out var sort) ? sort : _defaultSort;
        return orderBy(query, descending).ThenBy(x => x.Id);
    }

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    private sealed class ReplaceParameter(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : node;
    }
}

public static class ListSpec
{
    public static ListSpecBuilder<TEntity> For<TEntity>() where TEntity : EntityBase => new();
}

public sealed class ListSpecBuilder<TEntity> where TEntity : EntityBase
{
    private readonly List<Expression<Func<TEntity, string?>>> _searchFields = [];
    private readonly Dictionary<string, Func<IQueryable<TEntity>, bool, IOrderedQueryable<TEntity>>> _sorts = new(StringComparer.OrdinalIgnoreCase);
    private Func<IQueryable<TEntity>, bool, IOrderedQueryable<TEntity>>? _defaultSort;

    public ListSpecBuilder<TEntity> SearchIn(params Expression<Func<TEntity, string?>>[] fields)
    {
        _searchFields.AddRange(fields);
        return this;
    }

    public ListSpecBuilder<TEntity> SortBy<TKey>(string key, Expression<Func<TEntity, TKey>> field, bool isDefault = false)
    {
        IOrderedQueryable<TEntity> Order(IQueryable<TEntity> q, bool descending) =>
            descending ? q.OrderByDescending(field) : q.OrderBy(field);
        _sorts[key] = Order;
        if (isDefault) _defaultSort = Order;
        return this;
    }

    /// <summary>Default order when no (known) sort key is given; may ignore the direction.</summary>
    public ListSpecBuilder<TEntity> DefaultSort(Func<IQueryable<TEntity>, bool, IOrderedQueryable<TEntity>> order)
    {
        _defaultSort = order;
        return this;
    }

    public ListSpec<TEntity, TResponse> Project<TResponse>(Expression<Func<TEntity, TResponse>> projection) =>
        new(_searchFields, _sorts, _defaultSort ?? throw new InvalidOperationException("A list needs a default sort."), projection);
}
