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
    Task<List<ChatThread>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<ChatThread?> GetActiveByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ChatThread?> AddAsync(ChatThread entity, CancellationToken cancellationToken = default);

    Task<ChatThread?> UpdateAgentAsync(
        Guid id,
        Guid userId,
        ChatAgent chatAgent,
        Guid updatedBy,
        CancellationToken cancellationToken = default);

    Task<ChatThread?> UpdateTitleAsync(
        Guid id,
        Guid userId,
        string title,
        Guid updatedBy,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, Guid userId, Guid updatedBy, CancellationToken cancellationToken = default);
}

#endregion

#region # ChatMessage

public interface IChatMessageRepository
{
    Task<List<ChatMessage>> GetByChatThreadIdAsync(Guid chatThreadId, CancellationToken cancellationToken = default);

    Task<List<ChatMessage>> GetRecentByChatThreadIdAsync(
        Guid chatThreadId,
        int limit,
        CancellationToken cancellationToken = default);

    Task<ChatMessage?> AddAsync(ChatMessage entity, CancellationToken cancellationToken = default);
}

#endregion

#region # Settings

public interface ISettingsRepository
{
    Task<User?> GetActiveUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<User?> UpdateUserProfileAsync(
        Guid userId,
        string firstName,
        string lastName,
        Guid updatedBy,
        CancellationToken cancellationToken = default);

    Task<bool> UserPasswordMatchesAsync(Guid userId, string password, CancellationToken cancellationToken = default);

    Task<bool> UpdateUserPasswordAsync(Guid userId, string newPassword, Guid updatedBy, CancellationToken cancellationToken = default);

    Task<EmailSettings?> GetUserEmailSettingsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<EmailSettings?> SaveUserEmailSettingsAsync(
        Guid userId,
        EmailSettings? emailSettings,
        Guid updatedBy,
        CancellationToken cancellationToken = default);
}

#endregion
