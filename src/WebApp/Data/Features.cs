using WebApp.Models;

namespace WebApp.Data;

public class Features(Persistence _repo)
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

    public async Task<AppResult<List<GetChatMessageResponse>?>> GetChatMessagesByThreadIdAsync(GetChatMessagesByThreadIdRequest getChatMessagesByThreadIdRequest)
    {
        var result = new AppResult<List<GetChatMessageResponse>?>();
        try
        {
            #region # Validate

            var hasError = result.Validate(getChatMessagesByThreadIdRequest);
            if (hasError)
            {
                return result;
            }

            #endregion

            #region # Execute

            var chatMessages = await _repo.GetChatMessagesByThreadIdAsync(getChatMessagesByThreadIdRequest);

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

    #endregion
}
