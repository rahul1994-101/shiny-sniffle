using WebApp.Features.Shared.Cqrs.Abstractions;

namespace WebApp.Features.Shared.Cqrs.Pipeline;

public sealed class FeaturePipeline<TRequest, TResult>(IEnumerable<IFeaturePipelineBehavior<TRequest, TResult>> behaviors)
    where TRequest : IFeatureRequest<TResult>
    where TResult : AppResult, new()
{
    public Task<TResult> ExecuteAsync(
        TRequest request,
        IFeatureHandler<TRequest, TResult> handler,
        CancellationToken cancellationToken)
    {
        FeatureHandlerDelegate<TRequest, TResult> pipeline =
            (req, ct) => handler.HandleAsync(req, ct);

        foreach (var behavior in behaviors.Reverse())
        {
            var next = pipeline;
            pipeline = (req, ct) => behavior.HandleAsync(req, next, ct);
        }

        return pipeline(request, cancellationToken);
    }
}
