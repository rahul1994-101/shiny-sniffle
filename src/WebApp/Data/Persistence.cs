using Microsoft.EntityFrameworkCore;

using WebApp.Models;

namespace WebApp.Data;

public sealed class Persistence(IDbContextFactory<AppDbContext> _dbContextFactory)
{
    #region # User

    public async Task<SignInResponse?> SignInAsync(SignInRequest signInRequest)
    {
        //var encPassword = signInRequest.Password.Encrypt();
        var encPassword = signInRequest.Password;

        await using var ctx = await _dbContextFactory.CreateDbContextAsync();
        var user = await ctx.Users
            .AsNoTracking()
            .Where(x =>
                x.Email.ToLower() == signInRequest.EmailId.ToLower() &&
                x.Password.ToLower() == encPassword.ToLower() &&
                x.IsActive == true &&
                x.IsDeleted == false
            )
            .Select(x => new SignInResponse
            {
                Id = x.Id,
                Email = x.Email,
                FullName = x.FirstName + " " + x.LastName
            })
            .FirstOrDefaultAsync();

        return user;
    }

    #endregion

    #region # ChatThread

    public async Task<List<ChatThreadDto>?> GetChatThreadsByUserIdAsync(Guid userId)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync();
        var chatThreads = await ctx.ChatThreads
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId &&
                x.IsActive == true &&
                x.IsDeleted == false
            )
            .OrderByDescending(x => x.UpdatedAt)
            .Select(x => new ChatThreadDto
            {
                Id = x.Id,
                Title = x.Title,
                UserId = x.UserId,
                ChatAgent = x.ChatAgent,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        return chatThreads;
    }

    public async Task<ChatThreadDto?> GetChatThreadByIdAsync(Guid id)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync();
        var chatThread = await ctx.ChatThreads
            .AsNoTracking()
            .Where(x =>
                x.Id == id &&
                x.IsActive == true &&
                x.IsDeleted == false
            )
            .Select(x => new ChatThreadDto
            {
                Id = x.Id,
                Title = x.Title,
                UserId = x.UserId,
                ChatAgent = x.ChatAgent,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync();

        return chatThread;
    }

    public async Task<ChatThreadDto?> AddChatThreadAsync(AddChatThreadRequest addChatThreadRequest)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync();

        var entity = new ChatThread
        {
            Title = addChatThreadRequest.Title,
            UserId = addChatThreadRequest.UserId,
            ChatAgent = addChatThreadRequest.ChatAgent,
            CreatedBy = addChatThreadRequest.UserId,
            UpdatedBy = addChatThreadRequest.UserId
        };

        await ctx.ChatThreads.AddAsync(entity);
        await ctx.SaveChangesAsync();

        var chatThread = ToChatThreadDto(entity);

        return chatThread;
    }

    public async Task<ChatThreadDto?> UpdateChatThreadAgentAsync(UpdateChatThreadAgentRequest updateChatThreadAgentRequest)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync();

        var entity = await ctx.ChatThreads
            .Where(x =>
                x.Id == updateChatThreadAgentRequest.Id &&
                x.UserId == updateChatThreadAgentRequest.UserId &&
                x.IsActive == true &&
                x.IsDeleted == false
            )
            .FirstOrDefaultAsync();

        if (entity is null)
        {
            return null;
        }

        entity.ChatAgent = updateChatThreadAgentRequest.ChatAgent;
        entity.UpdatedBy = updateChatThreadAgentRequest.UserId;
        entity.UpdatedAt = DateTime.UtcNow;

        await ctx.SaveChangesAsync();

        var chatThread = ToChatThreadDto(entity);

        return chatThread;
    }

    public async Task<ChatThreadDto?> UpdateChatThreadTitleAsync(UpdateChatThreadTitleRequest updateChatThreadTitleRequest)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync();

        var entity = await ctx.ChatThreads
            .Where(x =>
                x.Id == updateChatThreadTitleRequest.Id &&
                x.UserId == updateChatThreadTitleRequest.UserId &&
                x.IsActive == true &&
                x.IsDeleted == false
            )
            .FirstOrDefaultAsync();

        if (entity is null)
        {
            return null;
        }

        entity.Title = updateChatThreadTitleRequest.Title;
        entity.UpdatedBy = updateChatThreadTitleRequest.UserId;
        entity.UpdatedAt = DateTime.UtcNow;

        await ctx.SaveChangesAsync();

        var chatThread = ToChatThreadDto(entity);

        return chatThread;
    }

    public async Task<bool> DeleteChatThreadAsync(DeleteChatThreadRequest deleteChatThreadRequest)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync();

        var entity = await ctx.ChatThreads
            .Where(x =>
                x.Id == deleteChatThreadRequest.Id &&
                x.UserId == deleteChatThreadRequest.UserId &&
                x.IsActive == true &&
                x.IsDeleted == false
            )
            .FirstOrDefaultAsync();

        if (entity is null)
        {
            return false;
        }

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.UpdatedBy = deleteChatThreadRequest.UserId;
        entity.UpdatedAt = DateTime.UtcNow;

        await ctx.SaveChangesAsync();

        return true;
    }

    #endregion

    #region # ChatMessage

    public async Task<List<ChatMessageDto>?> GetChatMessagesByChatThreadIdAsync(Guid chatThreadId)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync();
        var chatMessages = await ctx.ChatMessages
            .AsNoTracking()
            .Where(x =>
                x.ChatThreadId == chatThreadId &&
                x.IsActive == true &&
                x.IsDeleted == false
            )
            .OrderBy(x => x.CreatedAt)
            .Select(x => new ChatMessageDto
            {
                Id = x.Id,
                ChatThreadId = x.ChatThreadId,
                Role = x.Role,
                Content = x.Content,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return chatMessages;
    }

    public async Task<List<ChatMessageDto>?> GetRecentChatMessagesByChatThreadIdAsync(Guid chatThreadId, int limit)
    {
        var take = Math.Clamp(limit, 1, 100);

        await using var ctx = await _dbContextFactory.CreateDbContextAsync();
        var chatMessages = await ctx.ChatMessages
            .AsNoTracking()
            .Where(x =>
                x.ChatThreadId == chatThreadId &&
                x.IsActive == true &&
                x.IsDeleted == false
            )
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new ChatMessageDto
            {
                Id = x.Id,
                ChatThreadId = x.ChatThreadId,
                Role = x.Role,
                Content = x.Content,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return chatMessages;
    }

    public async Task<ChatMessageDto?> AddChatMessageAsync(ChatMessage entity)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync();

        await ctx.ChatMessages.AddAsync(entity);
        await ctx.SaveChangesAsync();

        var chatMessage = ToChatMessageDto(entity);

        return chatMessage;
    }

    #endregion

    #region # Private Helpers

    private static ChatThreadDto ToChatThreadDto(ChatThread entity) =>
        new()
        {
            Id = entity.Id,
            Title = entity.Title,
            UserId = entity.UserId,
            ChatAgent = entity.ChatAgent,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };

    private static ChatMessageDto ToChatMessageDto(ChatMessage entity) =>
        new()
        {
            Id = entity.Id,
            ChatThreadId = entity.ChatThreadId,
            Role = entity.Role,
            Content = entity.Content,
            CreatedAt = entity.CreatedAt
        };

    #endregion
}
