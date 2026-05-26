using WebApp.AI.Contracts;
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

    public async Task<AppResult<List<GetChatThreadResponse>?>> GetChatThreadsByUserIdAsync(GetChatThreadsByUserIdRequest getChatThreadsByUserIdRequest)
    {
        var result = new AppResult<List<GetChatThreadResponse>?>();
        try
        {
            #region # Validate

            var hasError = result.Validate(getChatThreadsByUserIdRequest);
            if (hasError)
            {
                return result;
            }

            #endregion

            #region # Execute

            var chatThreads = await _repo.GetChatThreadsByUserIdAsync(getChatThreadsByUserIdRequest);

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

    public async Task<AppResult<GetChatThreadResponse?>> GetChatThreadByIdAsync(GetChatThreadByIdRequest getChatThreadByIdRequest)
    {
        var result = new AppResult<GetChatThreadResponse?>();
        try
        {
            #region # Validate

            var hasError = result.Validate(getChatThreadByIdRequest);
            if (hasError)
            {
                return result;
            }

            #endregion

            #region # Execute

            var chatThread = await _repo.GetChatThreadByIdAsync(getChatThreadByIdRequest);

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

    public async Task<AppResult<List<GetChatMessageResponse>?>> GetChatMessagesByChatThreadIdAsync(GetChatMessagesByChatThreadIdRequest getChatMessagesByChatThreadIdRequest)
    {
        var result = new AppResult<List<GetChatMessageResponse>?>();
        try
        {
            #region # Validate

            var hasError = result.Validate(getChatMessagesByChatThreadIdRequest);
            if (hasError)
            {
                return result;
            }

            #endregion

            #region # Execute

            var chatMessages = await _repo.GetChatMessagesByChatThreadIdAsync(getChatMessagesByChatThreadIdRequest);

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

    public async Task<AppResult<AddChatMessageResponse?>> AddChatMessageAsync(AddChatMessageRequest addChatMessageRequest)
    {
        var result = new AppResult<AddChatMessageResponse?>();
        try
        {
            #region # Validate

            var hasError = result.Validate(addChatMessageRequest);
            if (hasError)
            {
                return result;
            }

            #endregion

            #region # Execute

            var chatMessage = await _repo.AddChatMessageAsync(addChatMessageRequest);

            #endregion

            #region # Handle Result

            if (chatMessage is null)
            {
                result.Failure(ErrorCode.InternalServerError, "Failed to create chat message.");
            }
            else
            {
                result.Success(chatMessage);
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

            #endregion

            #region # Execute

            var turn = await _chatOrchestrator.ProcessTurnAsync(new ChatTurnRequest
            {
                ChatThreadId = processChatTurnRequest.ChatThreadId,
                UserId = processChatTurnRequest.UserId,
                UserMessage = processChatTurnRequest.Message.Trim()
            });

            #endregion

            #region # Handle Result

            result.Success(new ProcessChatTurnResponse
            {
                AssistantContent = turn.AssistantContent,
                Intent = turn.Intent,
                Handler = turn.Handler,
                ModelDeployment = turn.ModelDeployment
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
}
