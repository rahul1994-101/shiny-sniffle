using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

using AiChatMessage = Microsoft.Extensions.AI.ChatMessage;

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

    public ChatAgent ChatAgent { get; set; }
}

public class AddChatThreadResponse
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public ChatAgent ChatAgent { get; set; }

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

    public ChatAgent ChatAgent { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

public class UpdateChatThreadAgentRequest
{
    [Required(ErrorMessage = "Thread Id is required.")]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "User Id is required.")]
    public Guid UserId { get; set; }

    public ChatAgent ChatAgent { get; set; }
}

public class UpdateChatThreadAgentResponse
{
    public Guid Id { get; set; }

    public ChatAgent ChatAgent { get; set; }

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
    [Required(ErrorMessage = "Chat Thread Id is required.")]
    public Guid ChatThreadId { get; set; }

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

    public Guid ChatThreadId { get; set; }

    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

public class GetChatMessagesByChatThreadIdRequest
{
    [Required(ErrorMessage = "Chat Thread Id is required.")]
    public Guid ChatThreadId { get; set; }
}

public class GetChatMessageResponse
{
    public Guid Id { get; set; }

    public Guid ChatThreadId { get; set; }

    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

public class ProcessChatTurnRequest
{
    [Required(ErrorMessage = "Chat Thread Id is required.")]
    public Guid ChatThreadId { get; set; }

    [Required(ErrorMessage = "User Id is required.")]
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "Message is required.")]
    public string Message { get; set; } = string.Empty;
}

public class ProcessChatTurnResponse
{
    public string AssistantContent { get; set; } = string.Empty;
}

#endregion

#region # AI

public sealed class FoundryOptions
{
    public const string SectionName = "Foundry";

    public bool Enabled { get; set; }

    /// <summary>
    /// Azure OpenAI resource base URL (maps to AZURE_OPENAI_ENDPOINT without deployment path).
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Azure OpenAI API key (maps to AZURE_OPENAI_API_KEY).
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Optional API version override (maps to AZURE_OPENAI_API_VERSION).
    /// </summary>
    public string ApiVersion { get; set; } = string.Empty;

    public bool IsConfigured =>
        Enabled &&
        !string.IsNullOrWhiteSpace(Endpoint) &&
        !string.IsNullOrWhiteSpace(ApiKey);
}

public sealed class ChatTurnRequest
{
    public Guid UserId { get; init; }

    public Guid ChatThreadId { get; init; }

    public string UserMessage { get; init; } = string.Empty;
}

public sealed class ChatTurnResult
{
    public string AssistantContent { get; init; } = string.Empty;
}

public sealed class IntentResult
{
    public string Intent { get; set; } = IntentKeys.GeneralChat;

    public double Confidence { get; set; }

    public string Reason { get; set; } = string.Empty;
}

public static class IntentKeys
{
    public const string GeneralChat = "general.chat";
}

public sealed class MemoryContext
{
    public Guid ChatThreadId { get; init; }

    public IReadOnlyList<AiChatMessage> Messages { get; init; } = [];

    public List<AiChatMessage> ToChatMessages() => Messages.ToList();
}

#endregion
