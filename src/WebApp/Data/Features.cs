using System.Text.Json;

using WebApp.AI;
using WebApp.Models;
using WebApp.Utilities.Extensions;

namespace WebApp.Data;

public class Features(Persistence _repo, ChatOrchestrator _chatOrchestrator)
{
    #region # User

    public async Task<AppResult<SignInResponse?>> SignInAsync(SignInRequest signInRequest)
    {
        var result = new AppResult<SignInResponse?>();
        try
        {
            #region # Validate

            var hasError = result.Validate(signInRequest);
            if (hasError)
            {
                return result;
            }

            #endregion

            #region # Execute

            var user = await _repo.SignInAsync(signInRequest);

            #endregion

            #region # Handle Result

            if (user is null)
            {
                result.Failure(ErrorCode.NotFound, "Invalid Credentials");
            }
            else
            {
                result.Success(user);
            }

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    #endregion

    #region # ChatThread

    public async Task<AppResult<List<ChatThreadDto>?>> GetChatThreadsByUserIdAsync(Guid userId)
    {
        var result = new AppResult<List<ChatThreadDto>?>();
        try
        {
            #region # Validate

            if (userId == Guid.Empty)
            {
                result.Failure(ErrorCode.BadRequest, "User Id is required.");
                return result;
            }

            #endregion

            #region # Execute

            var chatThreads = await _repo.GetChatThreadsByUserIdAsync(userId);

            #endregion

            #region # Handle Result

            if (chatThreads is null)
            {
                result.Failure(ErrorCode.InternalServerError, "Failed to fetch chat threads.");
            }
            else
            {
                result.Success(chatThreads);
            }

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    public async Task<AppResult<ChatThreadDto?>> GetChatThreadByIdAsync(Guid id)
    {
        var result = new AppResult<ChatThreadDto?>();
        try
        {
            #region # Validate

            if (id == Guid.Empty)
            {
                result.Failure(ErrorCode.BadRequest, "Thread Id is required.");
                return result;
            }

            #endregion

            #region # Execute

            var chatThread = await _repo.GetChatThreadByIdAsync(id);

            #endregion

            #region # Handle Result

            if (chatThread is null)
            {
                result.Failure(ErrorCode.NotFound, "Chat thread not found.");
            }
            else
            {
                result.Success(chatThread);
            }

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    public async Task<AppResult<ChatThreadDto?>> AddChatThreadAsync(AddChatThreadRequest addChatThreadRequest)
    {
        var result = new AppResult<ChatThreadDto?>();
        try
        {
            #region # Validate

            var hasError = result.Validate(addChatThreadRequest);
            if (hasError)
            {
                return result;
            }

            #endregion

            #region # Execute

            var chatThread = await _repo.AddChatThreadAsync(addChatThreadRequest);

            #endregion

            #region # Handle Result

            if (chatThread is null)
            {
                result.Failure(ErrorCode.InternalServerError, "Failed to create chat thread.");
            }
            else
            {
                result.Success(chatThread);
            }

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    public async Task<AppResult<ChatThreadDto?>> UpdateChatThreadTitleAsync(UpdateChatThreadTitleRequest updateChatThreadTitleRequest)
    {
        var result = new AppResult<ChatThreadDto?>();
        try
        {
            #region # Validate

            var hasError = result.Validate(updateChatThreadTitleRequest);
            if (hasError)
            {
                return result;
            }

            #endregion

            #region # Execute

            var chatThread = await _repo.UpdateChatThreadTitleAsync(updateChatThreadTitleRequest);

            #endregion

            #region # Handle Result

            if (chatThread is null)
            {
                result.Failure(ErrorCode.NotFound, "Chat thread not found.");
            }
            else
            {
                result.Success(chatThread);
            }

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    public async Task<AppResult<ChatThreadDto?>> UpdateChatThreadAgentAsync(UpdateChatThreadAgentRequest updateChatThreadAgentRequest)
    {
        var result = new AppResult<ChatThreadDto?>();
        try
        {
            #region # Validate

            var hasError = result.Validate(updateChatThreadAgentRequest);
            if (hasError)
            {
                return result;
            }

            #endregion

            #region # Execute

            var chatThread = await _repo.UpdateChatThreadAgentAsync(updateChatThreadAgentRequest);

            #endregion

            #region # Handle Result

            if (chatThread is null)
            {
                result.Failure(ErrorCode.NotFound, "Chat thread not found.");
            }
            else
            {
                result.Success(chatThread);
            }

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    public async Task<AppResult> DeleteChatThreadAsync(DeleteChatThreadRequest deleteChatThreadRequest)
    {
        var result = new AppResult();
        try
        {
            #region # Validate

            var hasError = result.Validate(deleteChatThreadRequest);
            if (hasError)
            {
                return result;
            }

            #endregion

            #region # Execute

            var deleted = await _repo.DeleteChatThreadAsync(deleteChatThreadRequest);

            #endregion

            #region # Handle Result

            if (!deleted)
            {
                result.Failure(ErrorCode.NotFound, "Chat thread not found.");
            }
            else
            {
                result.Success();
            }

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    #endregion

    #region # ChatMessage

    public async Task<AppResult<List<ChatMessageDto>?>> GetChatMessagesByChatThreadIdAsync(Guid chatThreadId)
    {
        var result = new AppResult<List<ChatMessageDto>?>();
        try
        {
            #region # Validate

            if (chatThreadId == Guid.Empty)
            {
                result.Failure(ErrorCode.BadRequest, "Chat Thread Id is required.");
                return result;
            }

            #endregion

            #region # Execute

            var chatMessages = await _repo.GetChatMessagesByChatThreadIdAsync(chatThreadId);

            #endregion

            #region # Handle Result

            if (chatMessages is null)
            {
                result.Failure(ErrorCode.InternalServerError, "Failed to fetch chat messages.");
            }
            else
            {
                result.Success(chatMessages);
            }

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }
        return result;
    }

    public async Task<AppResult<SendChatMessageResponse?>> SendChatMessageAsync(SendChatMessageRequest sendChatMessageRequest)
    {
        var result = new AppResult<SendChatMessageResponse?>();
        try
        {
            #region # Validate

            var hasError = result.Validate(sendChatMessageRequest);
            if (hasError)
            {
                return result;
            }

            var text = sendChatMessageRequest.Message.Trim();
            if (string.IsNullOrEmpty(text))
            {
                result.Failure(ErrorCode.BadRequest, "Message is required.");
                return result;
            }

            var thread = await _repo.GetChatThreadByIdAsync(sendChatMessageRequest.ChatThreadId);
            if (thread is null || thread.UserId != sendChatMessageRequest.UserId)
            {
                result.Failure(ErrorCode.NotFound, "Chat thread not found.");
                return result;
            }

            var chatAgent = sendChatMessageRequest.ChatAgent == thread.ChatAgent
                ? sendChatMessageRequest.ChatAgent
                : thread.ChatAgent;

            #endregion

            #region # Execute

            var userMessage = await _repo.AddChatMessageAsync(new ChatMessage
            {
                ChatThreadId = sendChatMessageRequest.ChatThreadId,
                Role = ChatMessageRoles.User,
                Content = text,
                CreatedBy = sendChatMessageRequest.UserId,
                UpdatedBy = sendChatMessageRequest.UserId
            });
            if (userMessage is null)
            {
                result.Failure(ErrorCode.InternalServerError, "Failed to create chat message.");
                return result;
            }

            var agentRun = await _chatOrchestrator.RunChatAgentAsync(new RunChatAgentRequest
            {
                ChatThreadId = sendChatMessageRequest.ChatThreadId,
                UserId = sendChatMessageRequest.UserId,
                ChatAgent = chatAgent
            });

            var assistantMessage = await _repo.AddChatMessageAsync(new ChatMessage
            {
                ChatThreadId = sendChatMessageRequest.ChatThreadId,
                Role = ChatMessageRoles.Assistant,
                Content = agentRun.AssistantContent,
                CreatedBy = sendChatMessageRequest.UserId,
                UpdatedBy = sendChatMessageRequest.UserId
            });
            if (assistantMessage is null)
            {
                result.Failure(ErrorCode.InternalServerError, "Failed to create chat message.");
                return result;
            }

            #endregion

            #region # Handle Result

            result.Success(new SendChatMessageResponse
            {
                UserMessage = userMessage,
                AssistantMessage = assistantMessage
            });

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }

        return result;
    }

    #endregion

    #region # UserSetting

    private static readonly JsonSerializerOptions UserSettingsJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<AppResult<UserSettingsDto?>> GetUserSettingsAsync(Guid userId)
    {
        var result = new AppResult<UserSettingsDto?>();
        try
        {
            #region # Validate

            if (userId == Guid.Empty)
            {
                result.Failure(ErrorCode.BadRequest, "User Id is required.");
                return result;
            }

            #endregion

            #region # Execute

            var entity = await _repo.GetUserSettingByUserIdAsync(userId);

            #endregion

            #region # Handle Result

            result.Success(MapToUserSettingsDto(entity));

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }

        return result;
    }

    public async Task<AppResult<UserSettingsDto?>> SaveUserSettingsAsync(SaveUserSettingsRequest saveUserSettingsRequest)
    {
        var result = new AppResult<UserSettingsDto?>();
        try
        {
            #region # Validate

            var hasError = result.Validate(saveUserSettingsRequest);
            if (hasError)
            {
                return result;
            }

            var existing = await _repo.GetUserSettingByUserIdAsync(saveUserSettingsRequest.UserId);
            var existingEmail = DeserializeEmailSettings(existing?.EmailSettings);
            var emailValidationError = ValidateEmailSettingsForSave(
                saveUserSettingsRequest.Email,
                !string.IsNullOrWhiteSpace(existingEmail?.Password));
            if (emailValidationError is not null)
            {
                result.Failure(ErrorCode.BadRequest, emailValidationError);
                return result;
            }

            #endregion

            #region # Execute

            var emailJson = SerializeEmailSettingsForSave(
                saveUserSettingsRequest.Email,
                existingEmail?.Password);
            var now = DateTime.UtcNow;

            var entity = new UserSetting
            {
                UserId = saveUserSettingsRequest.UserId,
                EmailSettings = emailJson,
                CreatedBy = saveUserSettingsRequest.UserId,
                UpdatedBy = saveUserSettingsRequest.UserId,
                CreatedAt = now,
                UpdatedAt = now
            };

            var saved = await _repo.UpsertUserSettingAsync(entity);

            #endregion

            #region # Handle Result

            if (saved is null)
            {
                result.Failure(ErrorCode.InternalServerError, "Failed to save user settings.");
            }
            else
            {
                result.Success(MapToUserSettingsDto(saved));
            }

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }

        return result;
    }

    #region # UserSetting Helpers

    private static UserSettingsDto MapToUserSettingsDto(UserSetting? entity)
    {
        var emailStored = DeserializeEmailSettings(entity?.EmailSettings);

        return new UserSettingsDto
        {
            Email = MapEmailSettingsToDto(emailStored)
        };
    }

    private static EmailSettingsDto MapEmailSettingsToDto(EmailSettingsStoredDto? stored)
    {
        if (stored is null)
        {
            return new EmailSettingsDto();
        }

        return new EmailSettingsDto
        {
            EmailAddress = stored.EmailAddress,
            ImapHost = stored.ImapHost,
            ImapPort = stored.ImapPort,
            ImapUseSsl = stored.ImapUseSsl,
            SmtpHost = stored.SmtpHost,
            SmtpPort = stored.SmtpPort,
            SmtpUseSsl = stored.SmtpUseSsl,
            Username = stored.Username,
            Password = string.Empty,
            HasStoredPassword = !string.IsNullOrWhiteSpace(stored.Password)
        };
    }

    private static string? ValidateEmailSettingsForSave(EmailSettingsDto email, bool hasStoredPassword)
    {
        if (IsEmailSettingsEmpty(email))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(email.EmailAddress))
        {
            return "Email address is required for mailbox settings.";
        }

        if (string.IsNullOrWhiteSpace(email.ImapHost))
        {
            return "IMAP host is required for mailbox settings.";
        }

        if (string.IsNullOrWhiteSpace(email.SmtpHost))
        {
            return "SMTP host is required for mailbox settings.";
        }

        if (string.IsNullOrWhiteSpace(email.Username))
        {
            return "Mailbox username is required.";
        }

        if (string.IsNullOrWhiteSpace(email.Password) && !hasStoredPassword)
        {
            return "Mailbox password is required.";
        }

        return null;
    }

    private static bool IsEmailSettingsEmpty(EmailSettingsDto email) =>
        string.IsNullOrWhiteSpace(email.EmailAddress) &&
        string.IsNullOrWhiteSpace(email.ImapHost) &&
        string.IsNullOrWhiteSpace(email.SmtpHost) &&
        string.IsNullOrWhiteSpace(email.Username) &&
        string.IsNullOrWhiteSpace(email.Password);

    private static string? SerializeEmailSettingsForSave(EmailSettingsDto email, string? existingEncryptedPassword)
    {
        if (IsEmailSettingsEmpty(email))
        {
            return null;
        }

        var password = ResolveEmailPasswordForSave(email.Password, existingEncryptedPassword);
        var stored = new EmailSettingsStoredDto
        {
            EmailAddress = email.EmailAddress.Trim(),
            ImapHost = email.ImapHost.Trim(),
            ImapPort = email.ImapPort,
            ImapUseSsl = email.ImapUseSsl,
            SmtpHost = email.SmtpHost.Trim(),
            SmtpPort = email.SmtpPort,
            SmtpUseSsl = email.SmtpUseSsl,
            Username = email.Username.Trim(),
            Password = password
        };

        return JsonSerializer.Serialize(stored, UserSettingsJsonOptions);
    }

    private static string ResolveEmailPasswordForSave(string plainPassword, string? existingEncryptedPassword)
    {
        if (!string.IsNullOrWhiteSpace(plainPassword))
        {
            return plainPassword.Trim().Encrypt();
        }

        return existingEncryptedPassword ?? string.Empty;
    }

    private static EmailSettingsStoredDto? DeserializeEmailSettings(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<EmailSettingsStoredDto>(json, UserSettingsJsonOptions);
    }

    #endregion

    #endregion
}
