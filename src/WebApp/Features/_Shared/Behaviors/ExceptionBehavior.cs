using WebApp.Features._Shared.Abstractions;

namespace WebApp.Features._Shared.Behaviors;

public sealed class ExceptionBehavior<TRequest, TResult> : IFeaturePipelineBehavior<TRequest, TResult>
    where TRequest : IFeatureRequest<TResult>
    where TResult : AppResult, new()
{
    public async Task<TResult> HandleAsync(
        TRequest request,
        FeatureHandlerDelegate<TRequest, TResult> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next(request, cancellationToken);
        }
        catch (Exception ex)
        {
            var result = new TResult();
            result.Failure(ErrorCode.InternalServerError, ex.Message);
            return result;
        }
    }
}
