using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class ChatThreadRepository(IDbContextFactory<AppDbContext> _dbContextFactory) : IChatThreadRepository
{
    public async Task<List<ChatThread>> GetActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await ctx.ChatThreads
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId &&
                x.IsActive &&
                !x.IsDeleted)
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ChatThread?> GetActiveByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await ctx.ChatThreads
            .AsNoTracking()
            .Where(x =>
                x.Id == id &&
                x.IsActive &&
                !x.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ChatThread?> AddAsync(ChatThread entity, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        await ctx.ChatThreads.AddAsync(entity, cancellationToken);
        await ctx.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<ChatThread?> UpdateAgentAsync(
        Guid id,
        Guid userId,
        ChatAgent chatAgent,
        Guid updatedBy,
        CancellationToken cancellationToken = default)
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
        return entity;
    }

    public async Task<ChatThread?> UpdateTitleAsync(
        Guid id,
        Guid userId,
        string title,
        Guid updatedBy,
        CancellationToken cancellationToken = default)
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
        return entity;
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        Guid userId,
        Guid updatedBy,
        CancellationToken cancellationToken = default)
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

    private static Task<ChatThread?> FindActiveTrackedAsync(
        AppDbContext ctx,
        Guid id,
        Guid userId,
        CancellationToken cancellationToken) =>
        ctx.ChatThreads
            .Where(x =>
                x.Id == id &&
                x.UserId == userId &&
                x.IsActive &&
                !x.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
}
