using MediatR.Abstractions;
using MediatR.Results;

namespace MediatR.Behaviors;

public sealed class ExceptionBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
    where TRequest : IRequest<TResult>
    where TResult : Result, new()
{
    public async ValueTask<TResult> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TRequest, TResult> next,
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
