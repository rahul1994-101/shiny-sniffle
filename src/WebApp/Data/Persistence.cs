using Microsoft.EntityFrameworkCore;

using WebApp.Models;
using WebApp.Utilities.Helpers;

namespace WebApp.Data;

public sealed class Persistence(AppDbContext _ctx)
{
    #region # User

    public async Task<SignInResponse?> SignInAsync(SignInRequest signInRequest)
    {
        return new SignInResponse
        {
            Id = Guid.NewGuid(),
            Email = signInRequest.EmailId,
            FullName = "John Doe"
        };

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

    public async Task<AddChatThreadResponse?> AddChatThreadAsync(AddChatThreadRequest addChatThreadRequest)
    {
        return new AddChatThreadResponse
        {
            Id = Guid.NewGuid(),
            Title = addChatThreadRequest.Title,
            UserId = addChatThreadRequest.UserId,
            CreatedAt = DateTime.UtcNow
        };

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

    public async Task<List<GetChatThreadResponse>?> GetChatThreadsByUserIdAsync(GetChatThreadsByUserIdRequest getChatThreadsByUserIdRequest)
    {
        return new List<GetChatThreadResponse>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Mock Thread A",
                UserId = getChatThreadsByUserIdRequest.UserId,
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                UpdatedAt = DateTime.UtcNow.AddMinutes(-10)
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Mock Thread B",
                UserId = getChatThreadsByUserIdRequest.UserId,
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                UpdatedAt = DateTime.UtcNow.AddMinutes(-1)
            }
        };

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
        return new GetChatThreadResponse
        {
            Id = getChatThreadByIdRequest.Id,
            Title = "Mock Thread",
            UserId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            UpdatedAt = DateTime.UtcNow
        };

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

    public async Task<UpdateChatThreadTitleResponse?> UpdateChatThreadTitleAsync(UpdateChatThreadTitleRequest updateChatThreadTitleRequest)
    {
        return new UpdateChatThreadTitleResponse
        {
            Id = updateChatThreadTitleRequest.Id,
            Title = updateChatThreadTitleRequest.Title,
            UserId = updateChatThreadTitleRequest.UserId,
            UpdatedAt = DateTime.UtcNow
        };

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
        return new DeleteChatThreadResponse
        {
            Id = deleteChatThreadRequest.Id
        };

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

    public async Task<AddChatMessageResponse?> AddChatMessageAsync(AddChatMessageRequest addChatMessageRequest)
    {
        return new AddChatMessageResponse
        {
            Id = Guid.NewGuid(),
            ThreadId = addChatMessageRequest.ThreadId,
            Role = addChatMessageRequest.Role,
            Content = addChatMessageRequest.Content,
            CreatedAt = DateTime.UtcNow
        };

        var entity = new ChatMessage
        {
            ThreadId = addChatMessageRequest.ThreadId,
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
            ThreadId = entity.ThreadId,
            Role = entity.Role,
            Content = entity.Content,
            CreatedAt = entity.CreatedAt
        };
    }

    public async Task<List<GetChatMessageResponse>?> GetChatMessagesByThreadIdAsync(GetChatMessagesByThreadIdRequest getChatMessagesByThreadIdRequest)
    {
        return new List<GetChatMessageResponse>();

        return await _ctx.ChatMessages
            .AsNoTracking()
            .Where(x =>
                x.ThreadId == getChatMessagesByThreadIdRequest.ThreadId &&
                x.IsActive == true &&
                x.IsDeleted == false
            )
            .OrderBy(x => x.CreatedAt)
            .Select(x => new GetChatMessageResponse
            {
                Id = x.Id,
                ThreadId = x.ThreadId,
                Role = x.Role,
                Content = x.Content,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();
    }

    #endregion
}
