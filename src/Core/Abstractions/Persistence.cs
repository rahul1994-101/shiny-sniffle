using Core.DTOs;

namespace Core.Abstractions;

#region # User

public interface IUserRepository
{
    Task<User?> FindActiveByEmailAndPasswordAsync(
        string emailId,
        string password,
        CancellationToken cancellationToken = default);
}

#endregion

#region # ChatThread

public interface IChatThreadRepository
{
    Task<List<ChatThreadDto>?> GetChatThreadsByUserIdAsync(Guid userId);

    Task<ChatThreadDto?> GetChatThreadByIdAsync(Guid id);

    Task<ChatThreadDto?> AddChatThreadAsync(AddChatThreadRequest addChatThreadRequest);

    Task<ChatThreadDto?> UpdateChatThreadAgentAsync(UpdateChatThreadAgentRequest updateChatThreadAgentRequest);

    Task<ChatThreadDto?> UpdateChatThreadTitleAsync(UpdateChatThreadTitleRequest updateChatThreadTitleRequest);

    Task<bool> DeleteChatThreadAsync(DeleteChatThreadRequest deleteChatThreadRequest);
}

#endregion

#region # ChatMessage

public interface IChatMessageRepository
{
    Task<List<ChatMessageDto>?> GetChatMessagesByChatThreadIdAsync(Guid chatThreadId);

    Task<List<ChatMessageDto>?> GetRecentChatMessagesByChatThreadIdAsync(Guid chatThreadId, int limit);

    Task<ChatMessageDto?> AddChatMessageAsync(ChatMessage entity);
}

#endregion

#region # Settings

public interface ISettingsRepository
{
    Task<GeneralSettingsDto?> GetUserGeneralSettingsAsync(Guid userId);

    Task<GeneralSettingsDto?> UpdateUserProfileAsync(Guid userId, string firstName, string lastName, Guid updatedBy);

    Task<bool> UserPasswordMatchesAsync(Guid userId, string password);

    Task<bool> UpdateUserPasswordAsync(Guid userId, string newPassword, Guid updatedBy);

    Task<EmailSettings?> GetUserEmailSettingsAsync(Guid userId);

    Task<EmailSettings?> SaveUserEmailSettingsAsync(Guid userId, EmailSettings? emailSettings, Guid updatedBy);
}

#endregion
