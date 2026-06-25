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

#region # General

public class GeneralSettingsDto
{
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters.")]
    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}

public class SaveGeneralProfileRequest
{
    [Required(ErrorMessage = "User Id is required.")]
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters.")]
    public string LastName { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    [Required(ErrorMessage = "User Id is required.")]
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "Current password is required.")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [StringLength(255, MinimumLength = 6, ErrorMessage = "New password must be between 6 and 255 characters.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

#endregion

#region # Email

public class EmailSettingsDto
{
    public EmailProvider Provider { get; set; } = EmailProvider.Custom;

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

public class SaveEmailSettingsRequest
{
    [Required(ErrorMessage = "User Id is required.")]
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "Email settings are required.")]
    public EmailSettingsDto Email { get; set; } = null!;
}

#endregion

#endregion

#region # Mail

public sealed class InboxMessageSummary
{
    public uint Uid { get; init; }

    public string From { get; init; } = string.Empty;

    public string Subject { get; init; } = string.Empty;

    public DateTimeOffset Date { get; init; }

    public string? Snippet { get; init; }
}

public sealed class InboxQuery
{
    public DateTime? SinceUtc { get; init; }

    /// <summary>Exclusive upper bound for IMAP DeliveredBefore; null means no upper bound.</summary>
    public DateTime? UntilUtcExclusive { get; init; }

    public int Limit { get; init; } = 20;

    public bool CountOnly { get; init; }

    public bool UnreadOnly { get; init; }

    public string? FromContains { get; init; }

    public string? SubjectContains { get; init; }

    /// <summary>IMAP folder: empty/inbox, sent, drafts, trash, junk, or exact name from list_mailbox_folders.</summary>
    public string? Folder { get; init; }
}

public sealed class InboxListResult
{
    public IReadOnlyList<InboxMessageSummary> Messages { get; init; } = [];

    public int TotalMatched { get; init; }
}

public sealed class InboxMessageDetail
{
    public uint Uid { get; init; }

    public string From { get; init; } = string.Empty;

    public string Subject { get; init; } = string.Empty;

    public DateTimeOffset Date { get; init; }

    public string Body { get; init; } = string.Empty;

    public string Folder { get; init; } = "INBOX";

    public bool BodyFromHtml { get; init; }

    public IReadOnlyList<string> AttachmentNames { get; init; } = [];
}

public sealed class MailboxFolderInfo
{
    public string Name { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string? Role { get; init; }
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
