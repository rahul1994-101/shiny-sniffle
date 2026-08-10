using FluentValidation;
using Application.Features.chat.ChatMessages;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.chat.ChatMessages.Queries;

public sealed record GetChatMessagesByChatThreadIdRequest(Guid ChatThreadId)
    : IQuery<GetChatMessagesByChatThreadIdResponse>;

public sealed class GetChatMessagesByChatThreadIdResponse
{
    public List<ChatMessageDto> Messages { get; init; } = [];
}

public sealed class GetChatMessagesByChatThreadIdRequestValidator : AbstractValidator<GetChatMessagesByChatThreadIdRequest>
{
    public GetChatMessagesByChatThreadIdRequestValidator()
    {
        RuleFor(x => x.ChatThreadId)
            .NotEmpty()
            .WithMessage("Chat Thread Id is required.");
    }
}

public sealed class GetChatMessagesByChatThreadIdRequestHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<GetChatMessagesByChatThreadIdRequest, GetChatMessagesByChatThreadIdResponse>
{
    public async ValueTask<Result<GetChatMessagesByChatThreadIdResponse>> HandleAsync(GetChatMessagesByChatThreadIdRequest request, CancellationToken cancellationToken = default)
    {
        var result = new Result<GetChatMessagesByChatThreadIdResponse>();

        #region # Execute

        var messages = await GetByChatThreadIdAsync(request.ChatThreadId, cancellationToken);

        #endregion

        #region # Handle Result

        result.Success(new GetChatMessagesByChatThreadIdResponse { Messages = messages });

        #endregion

        return result;
    }

    #region # Private Helpers

    private async Task<List<ChatMessageDto>> GetByChatThreadIdAsync(Guid chatThreadId, CancellationToken cancellationToken)
    {
        await using var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken);
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

    #endregion
}
