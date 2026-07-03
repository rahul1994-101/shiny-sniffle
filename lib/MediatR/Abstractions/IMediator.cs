using MediatR.Results;

namespace MediatR.Abstractions;

public interface IMediator
{
    ValueTask<TResult> SendAsync<TRequest, TResult>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : IRequest<TResult>
        where TResult : Result, new();

    ValueTask<TResult> SendAsync<TResult>(
        IRequest<TResult> request,
        CancellationToken cancellationToken = default)
        where TResult : Result, new();

    ValueTask PublishAsync(INotification notification, CancellationToken cancellationToken = default);
}
