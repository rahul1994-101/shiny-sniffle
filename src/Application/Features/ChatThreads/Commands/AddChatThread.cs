using FluentValidation;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ChatThreads.Commands;

public sealed record AddChatThreadRequest(string Title, Guid UserId, ChatAgent ChatAgent = default)
    : ICommand<AddChatThreadResponse>;

public sealed class AddChatThreadResponse : ChatThreadDto
{
}

public sealed class AddChatThreadRequestValidator : AbstractValidator<AddChatThreadRequest>
{
    public AddChatThreadRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required.")
            .Length(1, 200)
            .WithMessage("Title must be between 1 and 200 characters.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");
    }
}

public sealed class AddChatThreadRequestHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<AddChatThreadRequest, AddChatThreadResponse>
{
    public async ValueTask<Result<AddChatThreadResponse>> HandleAsync(AddChatThreadRequest request, CancellationToken cancellationToken = default)
    {
        var result = new Result<AddChatThreadResponse>();

        #region # Execute

        var entity = new ChatThread
        {
            Title = request.Title,
            UserId = request.UserId,
            ChatAgent = request.ChatAgent,
            CreatedBy = request.UserId,
            UpdatedBy = request.UserId
        };

        var chatThread = await AddAsync(entity, cancellationToken);

        #endregion

        #region # Handle Result

        if (chatThread is null)
        {
            result.Failure(ErrorCode.InternalServerError, "Failed to create chat thread.");
        }
        else
        {
            result.Success(chatThread.AsResponse<AddChatThreadResponse>());
        }

        #endregion

        return result;
    }

    #region # Private Helpers

    private async Task<ChatThreadDto?> AddAsync(ChatThread entity, CancellationToken cancellationToken)
    {
        await using var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await ctx.ChatThreads.AddAsync(entity, cancellationToken);
        await ctx.SaveChangesAsync(cancellationToken);
        return ChatThreadDto.FromEntity(entity);
    }

    #endregion
}
