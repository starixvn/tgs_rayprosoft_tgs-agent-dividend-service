using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Tgs.Agent.Dividend.Domain;

public static class QueryableExtensions
{
    // Where extension for filters of any nullable type
    public static IQueryable<TSource> Where<TSource, TFilter>(this IQueryable<TSource> source,
        TFilter? filter,
        Expression<Func<TSource, bool>> predicate) where TFilter : struct
    {
        if (filter.HasValue) source = source.Where(predicate);

        return source;
    }

    // Where extension for string filters
    public static IQueryable<TSource> Where<TSource>(this IQueryable<TSource> source,
        string filter,
        Expression<Func<TSource, bool>> predicate)
    {
        if (!string.IsNullOrWhiteSpace(filter)) source = source.Where(predicate);

        return source;
    }

    // Where extension for collection filters
    public static IQueryable<TSource> Where<TSource, TFilter>(this IQueryable<TSource> source,
        IEnumerable<TFilter> filter,
        Expression<Func<TSource, bool>> predicate)
    {
        if (filter != null && filter.Any()) source = source.Where(predicate);

        return source;
    }

    public static IQueryable<TSource> Where<TSource>(this IQueryable<TSource> source,
        bool isFiltered,
        Expression<Func<TSource, bool>> predicate)
    {
        if (isFiltered) source = source.Where(predicate);

        return source;
    }

    public static IEnumerable<TSource> Where<TSource>(this IEnumerable<TSource> source,
        bool filter,
        Func<TSource, bool> predicate)
    {
        if (filter) source = source.Where(predicate);

        return source;
    }

    public static async Task<IEnumerable<T>> Where<T>(this IEnumerable<T> source, Func<T, Task<bool>> predicate)
    {
        var results = new ConcurrentQueue<T>();
        var tasks = source.Select(async x =>
        {
            if (await predicate(x)) results.Enqueue(x);
        });
        await Task.WhenAll(tasks);

        return results;
    }

    public static async Task<IEnumerable<T>> Where<T>(this IEnumerable<T> source,
        Func<T, ValueTask<bool>> predicate)
    {
        var results = new ConcurrentQueue<T>();
        var tasks = source.Select(async x =>
        {
            if (await predicate(x)) results.Enqueue(x);
        });
        await Task.WhenAll(tasks);

        return results;
    }
}