using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Chat.ChatMessages;

public sealed class ChatMessageRepository(IDbContextFactory<AppDbContext> _dbContextFactory)
{
    public async Task<List<ChatMessageDto>?> GetAllChatMessagesByThreadIdAsync(Guid userId, Guid threadId, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        if (!await IsThreadOwnedAsync(ctx, userId, threadId, cancellationToken))
        {
            return null;
        }

        var messages = await ctx.ChatMessages
            .AsNoTracking()
            .Where(x => x.ChatThreadId == threadId)
            .WhereActiveAndNotDeleted()
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return ChatMessageDto.FromEntities(messages);
    }

    public async Task<List<ChatMessageDto>> GetRecentChatMessagesByThreadIdForAIAsync(
        Guid userId,
        Guid threadId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(limit, 1, 100);

        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        if (!await IsThreadOwnedAsync(ctx, userId, threadId, cancellationToken))
        {
            return [];
        }

        var messages = await ctx.ChatMessages
            .AsNoTracking()
            .Where(x => x.ChatThreadId == threadId)
            .WhereActiveAndNotDeleted()
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return ChatMessageDto.FromEntities(messages);
    }

    public async Task<ChatMessageDto> AddAsync(ChatMessage entity, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        await ctx.ChatMessages.AddAsync(entity, cancellationToken);
        await ctx.SaveChangesAsync(cancellationToken);
        return ChatMessageDto.FromEntity(entity);
    }

    public async Task<int> GetChatMessageCountByThreadIdForAIAsync(Guid userId, Guid threadId, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        if (!await IsThreadOwnedAsync(ctx, userId, threadId, cancellationToken))
        {
            return 0;
        }

        return await ctx.ChatMessages
            .AsNoTracking()
            .Where(x => x.ChatThreadId == threadId)
            .WhereActiveAndNotDeleted()
            .CountAsync(cancellationToken);
    }

    public async Task<List<ChatMessageDto>> GetAllChatMessagesBeyondRecentWindowByThreadIdForAIAsync(
        Guid userId,
        Guid threadId,
        int recentLimit,
        CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(recentLimit, 1, 100);

        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        if (!await IsThreadOwnedAsync(ctx, userId, threadId, cancellationToken))
        {
            return [];
        }

        var recentIds = await ctx.ChatMessages
            .AsNoTracking()
            .Where(x => x.ChatThreadId == threadId)
            .WhereActiveAndNotDeleted()
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var messages = await ctx.ChatMessages
            .AsNoTracking()
            .Where(x => x.ChatThreadId == threadId && !recentIds.Contains(x.Id))
            .WhereActiveAndNotDeleted()
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return ChatMessageDto.FromEntities(messages);
    }

    private static Task<bool> IsThreadOwnedAsync(AppDbContext ctx, Guid userId, Guid threadId, CancellationToken cancellationToken) =>
        ctx.ChatThreads
            .AsNoTracking()
            .Where(x => x.Id == threadId && x.UserId == userId)
            .WhereActiveAndNotDeleted()
            .AnyAsync(cancellationToken);
}
