namespace AuthService.Application.Common;

/// <summary>
/// Marker interface for CQRS commands — state-mutating intents.
/// </summary>
public interface ICommand<out TResult>
{
}

public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
