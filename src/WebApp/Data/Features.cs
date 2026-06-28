using WebApp.AI;
using WebApp.Utilities.Helpers;
using WebApp.Utilities.Services;

namespace WebApp.Data;

public class Features(
    IUserRepository _users,
    IChatThreadRepository _chatThreads,
    IChatMessageRepository _chatMessages,
    ISettingsRepository _settings,
    ChatOrchestrator _chatOrchestrator,
    UserMailboxService _mailboxService)
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

            var user = await _users.SignInAsync(signInRequest);

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

            var chatThreads = await _chatThreads.GetChatThreadsByUserIdAsync(userId);

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

            var chatThread = await _chatThreads.GetChatThreadByIdAsync(id);

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

            var chatThread = await _chatThreads.AddChatThreadAsync(addChatThreadRequest);

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

            var chatThread = await _chatThreads.UpdateChatThreadTitleAsync(updateChatThreadTitleRequest);

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

            if (updateChatThreadAgentRequest.ChatAgent == ChatAgent.Email
                && !await _mailboxService.IsConfiguredAsync(updateChatThreadAgentRequest.UserId))
            {
                result.Failure(
                    ErrorCode.BadRequest,
                    "Connect your mailbox in Settings → Email before using the Email agent.");
                return result;
            }

            #endregion

            #region # Execute

            var chatThread = await _chatThreads.UpdateChatThreadAgentAsync(updateChatThreadAgentRequest);

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

            var deleted = await _chatThreads.DeleteChatThreadAsync(deleteChatThreadRequest);

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

            var chatMessages = await _chatMessages.GetChatMessagesByChatThreadIdAsync(chatThreadId);

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

            var thread = await _chatThreads.GetChatThreadByIdAsync(sendChatMessageRequest.ChatThreadId);
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

            var userMessage = await _chatMessages.AddChatMessageAsync(new ChatMessage
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

            var assistantMessage = await _chatMessages.AddChatMessageAsync(new ChatMessage
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

    #region # General

    public async Task<AppResult<GeneralSettingsDto?>> GetGeneralSettingsAsync(Guid userId)
    {
        var result = new AppResult<GeneralSettingsDto?>();
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

            var generalSettings = await _settings.GetUserGeneralSettingsAsync(userId);

            #endregion

            #region # Handle Result

            if (generalSettings is null)
            {
                result.Failure(ErrorCode.NotFound, "User not found.");
            }
            else
            {
                result.Success(generalSettings);
            }

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }

        return result;
    }

    public async Task<AppResult<GeneralSettingsDto?>> SaveGeneralProfileAsync(SaveGeneralProfileRequest saveGeneralProfileRequest)
    {
        var result = new AppResult<GeneralSettingsDto?>();
        try
        {
            #region # Validate

            if (result.Validate(saveGeneralProfileRequest))
            {
                return result;
            }

            #endregion

            #region # Execute

            var savedProfile = await _settings.UpdateUserProfileAsync(
                saveGeneralProfileRequest.UserId,
                saveGeneralProfileRequest.FirstName,
                saveGeneralProfileRequest.LastName,
                saveGeneralProfileRequest.UserId);

            #endregion

            #region # Handle Result

            if (savedProfile is null)
            {
                result.Failure(ErrorCode.NotFound, "User not found.");
            }
            else
            {
                result.Success(savedProfile);
            }

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }

        return result;
    }

    public async Task<AppResult> ChangePasswordAsync(ChangePasswordRequest changePasswordRequest)
    {
        var result = new AppResult();
        try
        {
            #region # Validate

            if (result.Validate(changePasswordRequest))
            {
                return result;
            }

            if (!string.Equals(
                    changePasswordRequest.NewPassword,
                    changePasswordRequest.ConfirmPassword,
                    StringComparison.Ordinal))
            {
                result.Failure(ErrorCode.BadRequest, "New password and confirmation do not match.");
                return result;
            }

            var currentMatches = await _settings.UserPasswordMatchesAsync(
                changePasswordRequest.UserId,
                changePasswordRequest.CurrentPassword);

            if (!currentMatches)
            {
                result.Failure(ErrorCode.BadRequest, "Current password is incorrect.");
                return result;
            }

            #endregion

            #region # Execute

            var updated = await _settings.UpdateUserPasswordAsync(
                changePasswordRequest.UserId,
                changePasswordRequest.NewPassword,
                changePasswordRequest.UserId);

            #endregion

            #region # Handle Result

            if (!updated)
            {
                result.Failure(ErrorCode.NotFound, "User not found.");
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

    #region # Email

    public async Task<AppResult<EmailSettingsDto?>> GetEmailSettingsAsync(Guid userId)
    {
        var result = new AppResult<EmailSettingsDto?>();
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

            var emailSettings = await _settings.GetUserEmailSettingsAsync(userId);

            #endregion

            #region # Handle Result

            result.Success(EmailSettingsHelpers.ToDto(emailSettings));

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }

        return result;
    }

    public async Task<AppResult<EmailSettingsDto?>> SaveEmailSettingsAsync(SaveEmailSettingsRequest saveEmailSettingsRequest)
    {
        var result = new AppResult<EmailSettingsDto?>();
        try
        {
            #region # Validate

            if (result.Validate(saveEmailSettingsRequest))
            {
                return result;
            }

            if (saveEmailSettingsRequest.Email is null)
            {
                result.Failure(ErrorCode.BadRequest, "Email settings are required.");
                return result;
            }

            if (result.Validate(saveEmailSettingsRequest.Email))
            {
                return result;
            }

            var existingSettings = await _settings.GetUserEmailSettingsAsync(saveEmailSettingsRequest.UserId);
            var validationError = EmailSettingsHelpers.TryBuildFromDto(
                saveEmailSettingsRequest.Email,
                existingSettings,
                EmailSettingsBuildMode.Save,
                out var newSettings);

            if (validationError is not null)
            {
                result.Failure(ErrorCode.BadRequest, validationError);
                return result;
            }

            #endregion

            #region # Execute

            var savedSettings = await _settings.SaveUserEmailSettingsAsync(
                saveEmailSettingsRequest.UserId,
                newSettings,
                saveEmailSettingsRequest.UserId);

            #endregion

            #region # Handle Result

            result.Success(EmailSettingsHelpers.ToDto(savedSettings));

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }

        return result;
    }

    public async Task<AppResult<MailboxTestResult?>> TestEmailConnectionAsync(Guid userId, EmailSettingsDto email)
    {
        var result = new AppResult<MailboxTestResult?>();
        try
        {
            #region # Validate

            if (userId == Guid.Empty)
            {
                result.Failure(ErrorCode.BadRequest, "User Id is required.");
                return result;
            }

            if (email is null)
            {
                result.Failure(ErrorCode.BadRequest, "Email settings are required.");
                return result;
            }

            #endregion

            #region # Execute

            var testResult = await _mailboxService.TestConnectionAsync(userId, email);

            #endregion

            #region # Handle Result

            result.Success(testResult);

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }

        return result;
    }

    #endregion

    #endregion
}
