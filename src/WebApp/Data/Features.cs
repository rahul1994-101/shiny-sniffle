using WebApp.AI.Orchestration;
using WebApp.Models;

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

    public async Task<AppResult<List<GetChatThreadResponse>?>> GetChatThreadsByUserIdAsync(Guid userId)
    {
        var result = new AppResult<List<GetChatThreadResponse>?>();
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

    public async Task<AppResult<GetChatThreadResponse?>> GetChatThreadByIdAsync(Guid id)
    {
        var result = new AppResult<GetChatThreadResponse?>();
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

    public async Task<AppResult<AddChatThreadResponse?>> AddChatThreadAsync(AddChatThreadRequest addChatThreadRequest)
    {
        var result = new AppResult<AddChatThreadResponse?>();
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

    public async Task<AppResult<UpdateChatThreadTitleResponse?>> UpdateChatThreadTitleAsync(UpdateChatThreadTitleRequest updateChatThreadTitleRequest)
    {
        var result = new AppResult<UpdateChatThreadTitleResponse?>();
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

    public async Task<AppResult<UpdateChatThreadAgentResponse?>> UpdateChatThreadAgentAsync(UpdateChatThreadAgentRequest updateChatThreadAgentRequest)
    {
        var result = new AppResult<UpdateChatThreadAgentResponse?>();
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

    public async Task<AppResult<DeleteChatThreadResponse?>> DeleteChatThreadAsync(DeleteChatThreadRequest deleteChatThreadRequest)
    {
        var result = new AppResult<DeleteChatThreadResponse?>();
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

            var chatThread = await _repo.DeleteChatThreadAsync(deleteChatThreadRequest);

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

    #endregion

    #region # ChatMessage

    public async Task<AppResult<List<GetChatMessageResponse>?>> GetChatMessagesByChatThreadIdAsync(Guid chatThreadId)
    {
        var result = new AppResult<List<GetChatMessageResponse>?>();
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

    public async Task<AppResult<ProcessChatTurnResponse?>> ProcessChatTurnAsync(ProcessChatTurnRequest processChatTurnRequest)
    {
        var result = new AppResult<ProcessChatTurnResponse?>();
        try
        {
            #region # Validate

            var hasError = result.Validate(processChatTurnRequest);
            if (hasError)
            {
                return result;
            }

            var text = processChatTurnRequest.Message.Trim();
            if (string.IsNullOrEmpty(text))
            {
                result.Failure(ErrorCode.BadRequest, "Message is required.");
                return result;
            }

            var thread = await _repo.GetChatThreadByIdAsync(processChatTurnRequest.ChatThreadId);
            if (thread is null || thread.UserId != processChatTurnRequest.UserId)
            {
                result.Failure(ErrorCode.NotFound, "Chat thread not found.");
                return result;
            }

            var chatAgent = processChatTurnRequest.ChatAgent == thread.ChatAgent
                ? processChatTurnRequest.ChatAgent
                : thread.ChatAgent;

            #endregion

            #region # Execute

            var userMessage = await _repo.AddChatMessageAsync(new AddChatMessageRequest
            {
                ChatThreadId = processChatTurnRequest.ChatThreadId,
                Role = "user",
                Content = text,
                UserId = processChatTurnRequest.UserId
            });
            if (userMessage is null)
            {
                result.Failure(ErrorCode.InternalServerError, "Failed to create chat message.");
                return result;
            }

            var turn = await _chatOrchestrator.ProcessTurnAsync(new ChatTurnRequest
            {
                ChatThreadId = processChatTurnRequest.ChatThreadId,
                UserId = processChatTurnRequest.UserId,
                ChatAgent = chatAgent
            });

            var assistantMessage = await _repo.AddChatMessageAsync(new AddChatMessageRequest
            {
                ChatThreadId = processChatTurnRequest.ChatThreadId,
                Role = "assistant",
                Content = turn.AssistantContent,
                UserId = processChatTurnRequest.UserId
            });
            if (assistantMessage is null)
            {
                result.Failure(ErrorCode.InternalServerError, "Failed to create chat message.");
                return result;
            }

            #endregion

            #region # Handle Result

            result.Success(new ProcessChatTurnResponse
            {
                UserMessage = ToGetChatMessageResponse(userMessage),
                AssistantMessage = ToGetChatMessageResponse(assistantMessage)
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

    #region # Private Helpers

    private static GetChatMessageResponse ToGetChatMessageResponse(AddChatMessageResponse message) =>
        new()
        {
            Id = message.Id,
            ChatThreadId = message.ChatThreadId,
            Role = message.Role,
            Content = message.Content,
            CreatedAt = message.CreatedAt
        };

    #endregion
}
