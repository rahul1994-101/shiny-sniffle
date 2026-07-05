using FluentValidation.Results;
using MediatR.Results;

namespace MediatR.Validation;

internal static class FluentValidationExtensions
{
    internal static TResult ToFailedResult<TResult>(ValidationResult validationResult)
        where TResult : Result, new()
    {
        var result = new TResult();
        foreach (var error in validationResult.Errors)
        {
            result.Failure(ErrorCode.BadRequest, error.ErrorMessage);
        }

        return result;
    }
}
