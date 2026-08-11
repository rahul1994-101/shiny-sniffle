using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.chat.ChatThreads;

public sealed class ChatThreadRepository(IDbContextFactory<AppDbContext> _dbContextFactory)
{
    public async Task<ChatThreadDto?> GetActiveByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var thread = await ctx.ChatThreads
            .AsNoTracking()
            .Where(x =>
                x.Id == id &&
                x.IsActive &&
                !x.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        return thread is null ? null : ChatThreadDto.FromEntity(thread);
    }

    public async Task<ChatThreadDto?> UpdateTitleAsync(Guid id, Guid userId, string title, Guid updatedBy, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await FindActiveTrackedAsync(ctx, id, userId, cancellationToken);
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

    public async Task<ChatThreadDto?> UpdateAgentAsync(Guid id, Guid userId, ChatAgent chatAgent, Guid updatedBy, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await FindActiveTrackedAsync(ctx, id, userId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.ChatAgent = chatAgent;
        entity.UpdatedBy = updatedBy;
        entity.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync(cancellationToken);
        return ChatThreadDto.FromEntity(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId, Guid updatedBy, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await FindActiveTrackedAsync(ctx, id, userId, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.UpdatedBy = updatedBy;
        entity.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ThreadMemoryState?> GetMemoryStateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var thread = await ctx.ChatThreads
            .AsNoTracking()
            .Where(x =>
                x.Id == id &&
                x.IsActive &&
                !x.IsDeleted)
            .Select(x => new { x.MemorySummary, x.MemorySummaryThroughMessageId })
            .FirstOrDefaultAsync(cancellationToken);

        return thread is null
            ? null
            : new ThreadMemoryState(thread.MemorySummary, thread.MemorySummaryThroughMessageId);
    }

    public async Task<bool> UpdateMemorySummaryAsync(
        Guid id,
        Guid userId,
        string? summary,
        Guid? summaryThroughMessageId,
        Guid updatedBy,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await FindActiveTrackedAsync(ctx, id, userId, cancellationToken);
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

    private static Task<ChatThread?> FindActiveTrackedAsync(AppDbContext ctx, Guid id, Guid userId, CancellationToken cancellationToken) =>
        ctx.ChatThreads
            .Where(x =>
                x.Id == id &&
                x.UserId == userId &&
                x.IsActive &&
                !x.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
}
