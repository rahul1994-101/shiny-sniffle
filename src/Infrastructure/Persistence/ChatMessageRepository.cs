using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class ChatMessageRepository(IDbContextFactory<AppDbContext> _dbContextFactory) : IChatMessageRepository
{
    public async Task<List<ChatMessage>> GetByChatThreadIdAsync(
        Guid chatThreadId,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await ctx.ChatMessages
            .AsNoTracking()
            .Where(x =>
                x.ChatThreadId == chatThreadId &&
                x.IsActive &&
                !x.IsDeleted)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ChatMessage>> GetRecentByChatThreadIdAsync(
        Guid chatThreadId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(limit, 1, 100);

        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await ctx.ChatMessages
            .AsNoTracking()
            .Where(x =>
                x.ChatThreadId == chatThreadId &&
                x.IsActive &&
                !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ChatMessage?> AddAsync(ChatMessage entity, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        await ctx.ChatMessages.AddAsync(entity, cancellationToken);
        await ctx.SaveChangesAsync(cancellationToken);
        return entity;
    }
}
