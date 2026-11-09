namespace AuthService.Application.Common;

/// <summary>
/// Marker interface for CQRS queries — read-only intents.
/// </summary>
public interface IQuery<out TResult>
{
}

public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
