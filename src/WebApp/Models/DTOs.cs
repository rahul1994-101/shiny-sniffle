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

public class ChatThreadDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public ChatAgent ChatAgent { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

public class AddChatThreadRequest
{
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "User Id is required.")]
    public Guid UserId { get; set; }

    public ChatAgent ChatAgent { get; set; }
}

public class UpdateChatThreadAgentRequest
{
    [Required(ErrorMessage = "Thread Id is required.")]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "User Id is required.")]
    public Guid UserId { get; set; }

    public ChatAgent ChatAgent { get; set; }
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

public class DeleteChatThreadRequest
{
    [Required(ErrorMessage = "Thread Id is required.")]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "User Id is required.")]
    public Guid UserId { get; set; }
}

#endregion

#region # ChatMessage

public class ChatMessageDto
{
    public Guid Id { get; set; }

    public Guid ChatThreadId { get; set; }

    public string Role { get; set; } = ChatMessageRoles.User;

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

public class SendChatMessageRequest
{
    [Required(ErrorMessage = "Chat Thread Id is required.")]
    public Guid ChatThreadId { get; set; }

    [Required(ErrorMessage = "User Id is required.")]
    public Guid UserId { get; set; }

    public ChatAgent ChatAgent { get; set; }

    [Required(ErrorMessage = "Message is required.")]
    public string Message { get; set; } = string.Empty;
}

public class SendChatMessageResponse
{
    public ChatMessageDto UserMessage { get; set; } = null!;

    public ChatMessageDto AssistantMessage { get; set; } = null!;
}

#endregion

#region # UserSetting

public class EmailSettingsDto
{
    [StringLength(255, ErrorMessage = "Email address must be at most 255 characters.")]
    public string EmailAddress { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "IMAP host must be at most 255 characters.")]
    public string ImapHost { get; set; } = string.Empty;

    [Range(1, 65535, ErrorMessage = "IMAP port must be between 1 and 65535.")]
    public int ImapPort { get; set; } = 993;

    public bool ImapUseSsl { get; set; } = true;

    [StringLength(255, ErrorMessage = "SMTP host must be at most 255 characters.")]
    public string SmtpHost { get; set; } = string.Empty;

    [Range(1, 65535, ErrorMessage = "SMTP port must be between 1 and 65535.")]
    public int SmtpPort { get; set; } = 587;

    public bool SmtpUseSsl { get; set; } = true;

    [StringLength(255, ErrorMessage = "Username must be at most 255 characters.")]
    public string Username { get; set; } = string.Empty;

    /// <summary>Plain password on save only. Never returned on load.</summary>
    [StringLength(255, ErrorMessage = "Password must be at most 255 characters.")]
    public string Password { get; set; } = string.Empty;

    public bool HasStoredPassword { get; set; }
}

public class SaveUserSettingsRequest
{
    [Required(ErrorMessage = "User Id is required.")]
    public Guid UserId { get; set; }

    public SettingsSection Section { get; set; }

    public EmailSettingsDto? Email { get; set; }
}

#endregion

#region # Mail

public sealed class MailboxConnectionOptions
{
    public string Provider { get; init; } = "generic";

    public string EmailAddress { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string ImapHost { get; init; } = string.Empty;

    public int ImapPort { get; init; } = 993;

    public bool ImapUseSsl { get; init; } = true;

    public string SmtpHost { get; init; } = string.Empty;

    public int SmtpPort { get; init; } = 587;

    public bool SmtpUseSsl { get; init; } = true;
}

public sealed class InboxMessageSummary
{
    public string From { get; init; } = string.Empty;

    public string Subject { get; init; } = string.Empty;

    public DateTimeOffset Date { get; init; }

    public string? Snippet { get; init; }
}

public sealed class InboxQuery
{
    public DateTime? SinceUtc { get; init; }

    public int Limit { get; init; } = 20;
}

public sealed class OutboundMail
{
    public string To { get; init; } = string.Empty;

    public string Subject { get; init; } = string.Empty;

    public string Body { get; init; } = string.Empty;
}

public sealed class MailboxStatusResult
{
    public bool IsConfigured { get; init; }

    public bool IsReachable { get; init; }

    public string Message { get; init; } = string.Empty;
}

public sealed class MailboxTestResult
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;

    public bool ImapOk { get; init; }

    public bool SmtpOk { get; init; }
}

public sealed class SendMailResult
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;
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

public sealed class RunChatAgentRequest
{
    public Guid UserId { get; init; }

    public Guid ChatThreadId { get; init; }

    public ChatAgent ChatAgent { get; init; }
}

public sealed class RunChatAgentResponse
{
    public string AssistantContent { get; init; } = string.Empty;
}

#endregion
