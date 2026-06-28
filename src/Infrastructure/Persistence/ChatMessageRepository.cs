using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class ChatMessageRepository(IDbContextFactory<AppDbContext> _dbContextFactory) : IChatMessageRepository
{
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

        return ToChatMessageDto(entity);
    }

    private static ChatMessageDto ToChatMessageDto(ChatMessage entity) =>
        new()
        {
            Id = entity.Id,
            ChatThreadId = entity.ChatThreadId,
            Role = entity.Role,
            Content = entity.Content,
            CreatedAt = entity.CreatedAt
        };
}
