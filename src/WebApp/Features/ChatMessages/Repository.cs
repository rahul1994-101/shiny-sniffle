using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Features.ChatMessages;

public sealed class ChatMessageRepository(IDbContextFactory<AppDbContext> _dbContextFactory)
{
    public async Task<List<ChatMessageDto>> GetByChatThreadIdAsync(Guid chatThreadId, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var messages = await ctx.ChatMessages
            .AsNoTracking()
            .Where(x =>
                x.ChatThreadId == chatThreadId &&
                x.IsActive &&
                !x.IsDeleted)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return ChatMessageDto.FromEntities(messages);
    }

    public async Task<List<ChatMessageDto>> GetRecentByChatThreadIdAsync(Guid chatThreadId, int limit, CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(limit, 1, 100);

        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var messages = await ctx.ChatMessages
            .AsNoTracking()
            .Where(x =>
                x.ChatThreadId == chatThreadId &&
                x.IsActive &&
                !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return ChatMessageDto.FromEntities(messages);
    }

    public async Task<ChatMessageDto?> AddAsync(ChatMessage entity, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        await ctx.ChatMessages.AddAsync(entity, cancellationToken);
        await ctx.SaveChangesAsync(cancellationToken);
        return ChatMessageDto.FromEntity(entity);
    }
}
