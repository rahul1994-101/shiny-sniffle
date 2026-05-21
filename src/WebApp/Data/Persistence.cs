using Microsoft.EntityFrameworkCore;

using WebApp.Models;
using WebApp.Utilities.Helpers;

namespace WebApp.Data;

public sealed class Persistence(AppDbContext _ctx)
{
    #region # User

    public async Task<SignInResponse?> SignInAsync(SignInRequest signInRequest)
    {
        //var encPassword = signInRequest.Password.Encrypt();
        var encPassword = signInRequest.Password;

        return await _ctx.Users
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
    }

    #endregion

    #region # ChatThread

    public async Task<List<GetChatThreadResponse>?> GetChatThreadsByUserIdAsync(GetChatThreadsByUserIdRequest getChatThreadsByUserIdRequest)
    {
        return await _ctx.ChatThreads
            .AsNoTracking()
            .Where(x =>
                x.UserId == getChatThreadsByUserIdRequest.UserId &&
                x.IsActive == true &&
                x.IsDeleted == false
            )
            .OrderByDescending(x => x.UpdatedAt)
            .Select(x => new GetChatThreadResponse
            {
                Id = x.Id,
                Title = x.Title,
                UserId = x.UserId,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<GetChatThreadResponse?> GetChatThreadByIdAsync(GetChatThreadByIdRequest getChatThreadByIdRequest)
    {
        return await _ctx.ChatThreads
            .AsNoTracking()
            .Where(x =>
                x.Id == getChatThreadByIdRequest.Id &&
                x.IsActive == true &&
                x.IsDeleted == false
            )
            .Select(x => new GetChatThreadResponse
            {
                Id = x.Id,
                Title = x.Title,
                UserId = x.UserId,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<AddChatThreadResponse?> AddChatThreadAsync(AddChatThreadRequest addChatThreadRequest)
    {
        var entity = new ChatThread
        {
            Title = addChatThreadRequest.Title,
            UserId = addChatThreadRequest.UserId,
            CreatedBy = addChatThreadRequest.UserId,
            UpdatedBy = addChatThreadRequest.UserId
        };

        await _ctx.ChatThreads.AddAsync(entity);
        await _ctx.SaveChangesAsync();

        return new AddChatThreadResponse
        {
            Id = entity.Id,
            Title = entity.Title,
            UserId = entity.UserId,
            CreatedAt = entity.CreatedAt
        };
    }

    public async Task<UpdateChatThreadTitleResponse?> UpdateChatThreadTitleAsync(UpdateChatThreadTitleRequest updateChatThreadTitleRequest)
    {
        var entity = await _ctx.ChatThreads
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

        await _ctx.SaveChangesAsync();

        return new UpdateChatThreadTitleResponse
        {
            Id = entity.Id,
            Title = entity.Title,
            UserId = entity.UserId,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public async Task<DeleteChatThreadResponse?> DeleteChatThreadAsync(DeleteChatThreadRequest deleteChatThreadRequest)
    {
        var entity = await _ctx.ChatThreads
            .Where(x =>
                x.Id == deleteChatThreadRequest.Id &&
                x.UserId == deleteChatThreadRequest.UserId &&
                x.IsActive == true &&
                x.IsDeleted == false
            )
            .FirstOrDefaultAsync();

        if (entity is null)
        {
            return null;
        }

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.UpdatedBy = deleteChatThreadRequest.UserId;
        entity.UpdatedAt = DateTime.UtcNow;

        await _ctx.SaveChangesAsync();

        return new DeleteChatThreadResponse
        {
            Id = entity.Id
        };
    }

    #endregion

    #region # ChatMessage

    public async Task<List<GetChatMessageResponse>?> GetChatMessagesByChatThreadIdAsync(GetChatMessagesByChatThreadIdRequest getChatMessagesByChatThreadIdRequest)
    {
        return await _ctx.ChatMessages
            .AsNoTracking()
            .Where(x =>
                x.ChatThreadId == getChatMessagesByChatThreadIdRequest.ChatThreadId &&
                x.IsActive == true &&
                x.IsDeleted == false
            )
            .OrderBy(x => x.CreatedAt)
            .Select(x => new GetChatMessageResponse
            {
                Id = x.Id,
                ChatThreadId = x.ChatThreadId,
                Role = x.Role,
                Content = x.Content,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<AddChatMessageResponse?> AddChatMessageAsync(AddChatMessageRequest addChatMessageRequest)
    {
        var entity = new ChatMessage
        {
            ChatThreadId = addChatMessageRequest.ChatThreadId,
            Role = addChatMessageRequest.Role,
            Content = addChatMessageRequest.Content,
            CreatedBy = addChatMessageRequest.UserId,
            UpdatedBy = addChatMessageRequest.UserId
        };

        await _ctx.ChatMessages.AddAsync(entity);
        await _ctx.SaveChangesAsync();

        return new AddChatMessageResponse
        {
            Id = entity.Id,
            ChatThreadId = entity.ChatThreadId,
            Role = entity.Role,
            Content = entity.Content,
            CreatedAt = entity.CreatedAt
        };
    }

    #endregion
}
