using MediatR.Abstractions;
using MediatR.Dispatch;
using MediatR.Results;

namespace MediatR.Pipeline;

internal sealed class RequestPipeline<TRequest, TResult>(IEnumerable<IPipelineBehavior<TRequest, TResult>> behaviors)
    where TRequest : IRequest<TResult>
    where TResult : Result, new()
{
    public ValueTask<TResult> ExecuteAsync(
        TRequest request,
        IRequestExecutor<TRequest, TResult> handler,
        CancellationToken cancellationToken)
    {
        RequestHandlerDelegate<TRequest, TResult> pipeline =
            (req, ct) => handler.HandleAsync(req, ct);

        foreach (var behavior in behaviors.Reverse())
        {
            var next = pipeline;
            pipeline = (req, ct) => behavior.HandleAsync(req, next, ct);
        }

        return pipeline(request, cancellationToken);
    }
}
