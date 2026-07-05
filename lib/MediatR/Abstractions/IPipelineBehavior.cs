using MediatR.Results;

namespace MediatR.Abstractions;

public delegate ValueTask<TResult> RequestHandlerDelegate<TRequest, TResult>(
    TRequest request,
    CancellationToken cancellationToken)
    where TRequest : IRequest<TResult>
    where TResult : Result, new();

public interface IPipelineBehavior<TRequest, TResult>
    where TRequest : IRequest<TResult>
    where TResult : Result, new()
{
    ValueTask<TResult> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TRequest, TResult> next,
        CancellationToken cancellationToken);
}
