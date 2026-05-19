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

#region # User

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

    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;
}

#endregion

#region # ChatThread

public class AddChatThreadRequest
{
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "User Id is required.")]
    public Guid UserId { get; set; }
}

public class AddChatThreadResponse
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class GetChatThreadsByUserIdRequest
{
    [Required(ErrorMessage = "User Id is required.")]
    public Guid UserId { get; set; }
}

public class GetChatThreadByIdRequest
{
    [Required(ErrorMessage = "Thread Id is required.")]
    public Guid Id { get; set; }
}

public class GetChatThreadResponse
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

public class UpdateChatThreadTitleRequest
{
    [Required(ErrorMessage = "Thread Id is required.")]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "User Id is required.")]
    public Guid UserId { get; set; }
}

public class UpdateChatThreadTitleResponse
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public DateTime UpdatedAt { get; set; }
}

public class DeleteChatThreadRequest
{
    [Required(ErrorMessage = "Thread Id is required.")]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "User Id is required.")]
    public Guid UserId { get; set; }
}

public class DeleteChatThreadResponse
{
    public Guid Id { get; set; }
}

#endregion

#region # ChatMessage

public class AddChatMessageRequest
{
    [Required(ErrorMessage = "Thread Id is required.")]
    public Guid ThreadId { get; set; }

    [Required(ErrorMessage = "Role is required.")]
    [StringLength(20, MinimumLength = 1, ErrorMessage = "Role must be between 1 and 20 characters.")]
    public string Role { get; set; } = string.Empty;

    [Required(ErrorMessage = "Content is required.")]
    public string Content { get; set; } = string.Empty;

    [Required(ErrorMessage = "User Id is required.")]
    public Guid UserId { get; set; }
}

public class AddChatMessageResponse
{
    public Guid Id { get; set; }

    public Guid ThreadId { get; set; }

    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

public class GetChatMessagesByThreadIdRequest
{
    [Required(ErrorMessage = "Thread Id is required.")]
    public Guid ThreadId { get; set; }
}

public class GetChatMessageResponse
{
    public Guid Id { get; set; }

    public Guid ThreadId { get; set; }

    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

#endregion
