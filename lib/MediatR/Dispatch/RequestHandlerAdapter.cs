using MediatR.Abstractions;
using MediatR.Results;

namespace MediatR.Dispatch;

internal interface IRequestExecutor<in TRequest, TResult>
    where TRequest : IRequest<TResult>
    where TResult : Result, new()
{
    ValueTask<TResult> HandleAsync(TRequest request, CancellationToken cancellationToken);
}

internal sealed class RequestHandlerAdapter<TRequest, TResponse>(IRequestHandler<TRequest, TResponse> handler)
    : IRequestExecutor<TRequest, Result<TResponse>>
    where TRequest : IRequest<Result<TResponse>>
{
    public ValueTask<Result<TResponse>> HandleAsync(TRequest request, CancellationToken cancellationToken) =>
        handler.HandleAsync(request, cancellationToken);
}

internal sealed class CommandRequestHandlerAdapter<TRequest>(IRequestHandler<TRequest> handler)
    : IRequestExecutor<TRequest, Result>
    where TRequest : ICommand
{
    public ValueTask<Result> HandleAsync(TRequest request, CancellationToken cancellationToken) =>
        handler.HandleAsync(request, cancellationToken);
}
