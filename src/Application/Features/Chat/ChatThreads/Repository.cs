using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Chat.ChatThreads;

public sealed class ChatThreadRepository(IDbContextFactory<AppDbContext> _dbContextFactory)
{
    public async Task<List<ChatThreadDto>> GetAllChatThreadsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var threads = await ctx.ChatThreads
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId &&
                x.IsActive &&
                !x.IsDeleted)
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync(cancellationToken);

        return ChatThreadDto.FromEntities(threads);
    }

    public async Task<ChatThreadDto> AddAsync(ChatThread entity, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        await ctx.ChatThreads.AddAsync(entity, cancellationToken);
        await ctx.SaveChangesAsync(cancellationToken);
        return ChatThreadDto.FromEntity(entity);
    }

    public async Task<ChatThreadDto?> GetChatThreadByIdAsync(Guid userId, Guid threadId, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var thread = await ctx.ChatThreads
            .AsNoTracking()
            .Where(x =>
                x.Id == threadId &&
                x.UserId == userId &&
                x.IsActive &&
                !x.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        return thread is null ? null : ChatThreadDto.FromEntity(thread);
    }

    public async Task<ChatThreadDto?> UpdateTitleAsync(Guid userId, Guid threadId, string title, Guid updatedBy, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await FindActiveTrackedAsync(ctx, userId, threadId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Title = title;
        entity.UpdatedBy = updatedBy;
        entity.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync(cancellationToken);
        return ChatThreadDto.FromEntity(entity);
    }

    public async Task<ChatThreadDto?> UpdateAgentAsync(Guid userId, Guid threadId, ChatAgent chatAgent, Guid updatedBy, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await FindActiveTrackedAsync(ctx, userId, threadId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.ChatAgent = ChatAgentHelpers.ToPersistence(chatAgent);
        entity.UpdatedBy = updatedBy;
        entity.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync(cancellationToken);
        return ChatThreadDto.FromEntity(entity);
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid threadId, Guid updatedBy, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await FindActiveTrackedAsync(ctx, userId, threadId, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        var messages = await ctx.ChatMessages
            .Where(x =>
                x.ChatThreadId == threadId &&
                x.IsActive &&
                !x.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            message.IsDeleted = true;
            message.IsActive = false;
            message.UpdatedBy = updatedBy;
            message.UpdatedAt = now;
        }

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.MemorySummary = null;
        entity.MemorySummaryThroughMessageId = null;
        entity.UpdatedBy = updatedBy;
        entity.UpdatedAt = now;
        await ctx.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ThreadMemoryState?> GetMemoryStateAsync(Guid userId, Guid threadId, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var thread = await ctx.ChatThreads
            .AsNoTracking()
            .Where(x =>
                x.Id == threadId &&
                x.UserId == userId &&
                x.IsActive &&
                !x.IsDeleted)
            .Select(x => new { x.MemorySummary, x.MemorySummaryThroughMessageId })
            .FirstOrDefaultAsync(cancellationToken);

        return thread is null
            ? null
            : new ThreadMemoryState(thread.MemorySummary, thread.MemorySummaryThroughMessageId);
    }

    public async Task<bool> UpdateMemorySummaryAsync(
        Guid userId,
        Guid threadId,
        string? summary,
        Guid? summaryThroughMessageId,
        Guid updatedBy,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await FindActiveTrackedAsync(ctx, userId, threadId, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        entity.MemorySummary = summary;
        entity.MemorySummaryThroughMessageId = summaryThroughMessageId;
        entity.UpdatedBy = updatedBy;
        entity.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static Task<ChatThread?> FindActiveTrackedAsync(AppDbContext ctx, Guid userId, Guid threadId, CancellationToken cancellationToken) =>
        ctx.ChatThreads
            .Where(x =>
                x.Id == threadId &&
                x.UserId == userId &&
                x.IsActive &&
                !x.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
}
