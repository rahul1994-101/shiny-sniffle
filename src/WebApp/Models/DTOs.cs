using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace WebApp.Models;

#region # Common

public class AppResult
{
    #region # Init

    public AppResult()
    {
        HasError = false;
        Errors = new Collection<AppError>();
    }

    #endregion

    public bool HasError { get; protected set; }

    public Collection<AppError> Errors { get; }

    public virtual bool Validate<T>(T model) where T : class
    {
        if (model is null)
        {
            Failure(ErrorCode.BadRequest, $"{nameof(model)} can't be empty.");
        }
        else
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(model, null, null);

            var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);
            if (isValid)
            {
                Success();
            }
            else
            {
                foreach (var validationResult in validationResults)
                {
                    Failure(ErrorCode.BadRequest, validationResult.ErrorMessage ?? "Unknown Error!!");
                }
            }
        }
        return HasError;
    }

    public virtual void Success()
    {
        HasError = false;
        Errors.Clear();
    }

    public virtual void Failure(ErrorCode code, string message)
    {
        HasError = true;
        Errors.Add(new AppError(code, message));
    }
}

public class AppResult<T> : AppResult
{
    #region # Init

    public AppResult() : base() { }

    #endregion

    public T? Payload { get; protected set; }

    public void Success(T? payload)
    {
        base.Success();
        Payload = payload;
    }
}

public class AppError
{
    #region # Init

    public AppError() { }

    public AppError(ErrorCode code, string message)
    {
        Code = code;
        Message = message;
    }

    #endregion

    public ErrorCode Code { get; set; }
    public string Message { get; set; } = string.Empty;
}

#endregion

public class SignInRequest
{
    [Required(ErrorMessage = "Email Id is required.")]
    [StringLength(100, MinimumLength = 5, ErrorMessage = "Email Id must be between 5 and 100 characters.")]
    //[ValidEmailAddress]
    //[NoLeadingOrTrailingSpaces]
    public string EmailId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    //[StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters.")]
    //[NoSpaces]
    public string Password { get; set; } = string.Empty;
}

public class SignInResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
}
