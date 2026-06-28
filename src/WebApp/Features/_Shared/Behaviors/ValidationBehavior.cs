using FluentValidation;

using WebApp.Features._Shared.Abstractions;
using WebApp.Features._Shared.Validation;

namespace WebApp.Features._Shared.Behaviors;

public sealed class ValidationBehavior<TRequest, TResult>(IEnumerable<IValidator<TRequest>> validators)
    : IFeaturePipelineBehavior<TRequest, TResult>
    where TRequest : IFeatureRequest<TResult>
    where TResult : AppResult, new()
{
    public async Task<TResult> HandleAsync(
        TRequest request,
        FeatureHandlerDelegate<TRequest, TResult> next,
        CancellationToken cancellationToken)
    {
        foreach (var validator in validators)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return FluentValidationExtensions.ToFailedResult<TResult>(validationResult);
            }
        }

        return await next(request, cancellationToken);
    }
}
