using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace Vas.Infrastructure.Persistence.Mock;

/// <summary>
/// An in-memory query that also answers EF Core's async operators (ToListAsync, FirstOrDefaultAsync),
/// so a service's LINQ runs unchanged on the JSON rows and on the SQL endpoint.
/// </summary>
internal sealed class InMemoryAsyncQueryable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public InMemoryAsyncQueryable(IEnumerable<T> rows)
        : base(rows)
    {
    }

    public InMemoryAsyncQueryable(Expression expression)
        : base(expression)
    {
    }

    IQueryProvider IQueryable.Provider => new InMemoryAsyncQueryProvider(this);

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        new Enumerator(this.AsEnumerable().GetEnumerator());

    private sealed class Enumerator(IEnumerator<T> inner) : IAsyncEnumerator<T>
    {
        public T Current => inner.Current;

        public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(inner.MoveNext());

        public ValueTask DisposeAsync()
        {
            inner.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}

internal sealed class InMemoryAsyncQueryProvider(IQueryProvider inner) : IAsyncQueryProvider
{
    public IQueryable CreateQuery(Expression expression)
    {
        var elementType = expression.Type.GetGenericArguments()[0];
        return (IQueryable)Activator.CreateInstance(
            typeof(InMemoryAsyncQueryable<>).MakeGenericType(elementType), expression)!;
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
        new InMemoryAsyncQueryable<TElement>(expression);

    public object? Execute(Expression expression) => inner.Execute(expression);

    public TResult Execute<TResult>(Expression expression) => inner.Execute<TResult>(expression);

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        // TResult is Task<T>: run the query synchronously and wrap the value.
        var resultType = typeof(TResult).GetGenericArguments()[0];

        var value = typeof(IQueryProvider)
            .GetMethods()
            .Single(method => method is { Name: nameof(IQueryProvider.Execute), IsGenericMethod: true })
            .MakeGenericMethod(resultType)
            .Invoke(inner, [expression]);

        return (TResult)typeof(Task)
            .GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(resultType)
            .Invoke(null, [value])!;
    }
}
