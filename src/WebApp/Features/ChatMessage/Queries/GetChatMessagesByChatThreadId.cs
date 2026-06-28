using FluentValidation;

using WebApp.Features._Shared.Abstractions;

namespace WebApp.Features.ChatMessage.Queries;

public sealed record GetChatMessagesByChatThreadIdQuery(Guid ChatThreadId)
    : IQuery<AppResult<List<ChatMessageDto>?>>;

public sealed class GetChatMessagesByChatThreadIdQueryValidator : AbstractValidator<GetChatMessagesByChatThreadIdQuery>
{
    public GetChatMessagesByChatThreadIdQueryValidator()
    {
        RuleFor(x => x.ChatThreadId)
            .NotEmpty()
            .WithMessage("Chat Thread Id is required.");
    }
}

public sealed class GetChatMessagesByChatThreadIdQueryHandler(IChatMessageRepository chatMessages)
    : IFeatureHandler<GetChatMessagesByChatThreadIdQuery, AppResult<List<ChatMessageDto>?>>
{
    public async Task<AppResult<List<ChatMessageDto>?>> HandleAsync(
        GetChatMessagesByChatThreadIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = new AppResult<List<ChatMessageDto>?>();

        #region # Execute

        var messages = await chatMessages.GetChatMessagesByChatThreadIdAsync(query.ChatThreadId);

        #endregion

        #region # Handle Result

        if (messages is null)
        {
            result.Failure(ErrorCode.InternalServerError, "Failed to fetch chat messages.");
        }
        else
        {
            result.Success(messages);
        }

        #endregion

        return result;
    }
}
