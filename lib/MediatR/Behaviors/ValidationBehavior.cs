using FluentValidation;
using MediatR.Abstractions;
using MediatR.Results;
using MediatR.Validation;

namespace MediatR.Behaviors;

public sealed class ValidationBehavior<TRequest, TResult>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResult>
    where TRequest : IRequest<TResult>
    where TResult : Result, new()
{
    public async ValueTask<TResult> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TRequest, TResult> next,
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
