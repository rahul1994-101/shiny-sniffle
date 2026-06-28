using FluentValidation.Results;

namespace WebApp.Features._Shared.Validation;

internal static class FluentValidationExtensions
{
    internal static TResult ToFailedResult<TResult>(ValidationResult validationResult)
        where TResult : AppResult, new()
    {
        var result = new TResult();
        foreach (var error in validationResult.Errors)
        {
            result.Failure(ErrorCode.BadRequest, error.ErrorMessage);
        }

        return result;
    }
}
