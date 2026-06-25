using Microsoft.EntityFrameworkCore;

using Core.DTOs;
using Core.Entities;

namespace Infrastructure.Persistence;

public sealed class ChatThreadRepository(IDbContextFactory<AppDbContext> _dbContextFactory) : IChatThreadRepository
{
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

        return ToChatThreadDto(entity);
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

        return ToChatThreadDto(entity);
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

        return ToChatThreadDto(entity);
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
}
