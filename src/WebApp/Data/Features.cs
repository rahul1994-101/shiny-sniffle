using WebApp.AI;
using WebApp.Models;
using WebApp.Utilities.Helpers;

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

            var emailSettings = await _repo.GetUserEmailSettingsAsync(userId);

            #endregion

            #region # Handle Result

            result.Success(EmailSettingsHelpers.MapToDto(emailSettings));

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }

        return result;
    }

    public async Task<AppResult<EmailSettingsDto?>> SaveUserSettingsAsync(SaveUserSettingsRequest saveUserSettingsRequest)
    {
        var result = new AppResult<EmailSettingsDto?>();
        try
        {
            #region # Validate

            if (result.Validate(saveUserSettingsRequest))
            {
                return result;
            }

            if (saveUserSettingsRequest.UserId == Guid.Empty)
            {
                result.Failure(ErrorCode.BadRequest, "User Id is required.");
                return result;
            }

            switch (saveUserSettingsRequest.Section)
            {
                case SettingsSection.General:
                    result.Failure(ErrorCode.BadRequest, "General settings cannot be saved yet.");
                    return result;
                case SettingsSection.Email:
                    break;
                default:
                    result.Failure(ErrorCode.BadRequest, "Unsupported settings section.");
                    return result;
            }

            #endregion

            #region # Execute

            return saveUserSettingsRequest.Section switch
            {
                SettingsSection.Email => await SaveEmailSettingsCoreAsync(saveUserSettingsRequest.UserId, saveUserSettingsRequest.Email),
                _ => throw new InvalidOperationException("Unsupported settings section.")
            };

            #endregion
        }
        catch (Exception ex)
        {
            result.Failure(ErrorCode.InternalServerError, ex.Message);
        }

        return result;
    }

    private async Task<AppResult<EmailSettingsDto?>> SaveEmailSettingsCoreAsync(Guid userId, EmailSettingsDto? emailSettingsDto)
    {
        var result = new AppResult<EmailSettingsDto?>();

        #region # Validate

        if (emailSettingsDto is null)
        {
            result.Failure(ErrorCode.BadRequest, "Email settings are required.");
            return result;
        }

        if (result.Validate(emailSettingsDto))
        {
            return result;
        }

        var existingSettings = await _repo.GetUserEmailSettingsAsync(userId);
        var validationError = EmailSettingsHelpers.TryBuildForSave(emailSettingsDto, existingSettings, out var newSettings);
        if (validationError is not null)
        {
            result.Failure(ErrorCode.BadRequest, validationError);
            return result;
        }

        #endregion

        #region # Execute

        var savedSettings = await _repo.SaveUserEmailSettingsAsync(userId, newSettings, userId);

        #endregion

        #region # Handle Result

        result.Success(EmailSettingsHelpers.MapToDto(savedSettings));

        #endregion

        return result;
    }

    #endregion
}
