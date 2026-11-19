using System.Linq.Expressions;
using MakeupArt.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace MakeupArt.Infrastructure.Persistence;

public static class QueryableExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(request.Skip).Take(request.PageSize).ToListAsync(cancellationToken);
        return PagedResult<T>.Create(items, request.Page, request.PageSize, total);
    }

    public static IQueryable<T> ApplySort<T>(
        this IQueryable<T> query,
        PagedRequest request,
        IReadOnlyDictionary<string, Expression<Func<T, object>>> allowedSorts,
        Expression<Func<T, object>> defaultSort)
    {
        var sort = request.SortBy is not null && allowedSorts.TryGetValue(request.SortBy, out var expression)
            ? expression
            : defaultSort;
        return request.IsAscending ? query.OrderBy(sort) : query.OrderByDescending(sort);
    }

    public static IQueryable<T> WhereActive<T>(this IQueryable<T> query, PagedRequest request) where T : class
    {
        if (request.IsActive is null) return query;
        var property = typeof(T).GetProperty("IsActive");
        if (property is null || property.PropertyType != typeof(bool)) return query;
        var parameter = Expression.Parameter(typeof(T), "x");
        var body = Expression.Equal(Expression.Property(parameter, property), Expression.Constant(request.IsActive.Value));
        return query.Where(Expression.Lambda<Func<T, bool>>(body, parameter));
    }
}
