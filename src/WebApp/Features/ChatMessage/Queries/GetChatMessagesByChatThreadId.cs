using FluentValidation;

using WebApp.Features._Shared.Abstractions;

namespace WebApp.Features.ChatMessage.Queries;

public sealed record GetChatMessagesByChatThreadIdRequest(Guid ChatThreadId)
    : IQuery<AppResult<GetChatMessagesByChatThreadIdResponse?>>;

public sealed class GetChatMessagesByChatThreadIdRequestValidator : AbstractValidator<GetChatMessagesByChatThreadIdRequest>
{
    public GetChatMessagesByChatThreadIdRequestValidator()
    {
        RuleFor(x => x.ChatThreadId)
            .NotEmpty()
            .WithMessage("Chat Thread Id is required.");
    }
}

public sealed class GetChatMessagesByChatThreadIdRequestHandler(IChatMessageRepository chatMessages)
    : IFeatureHandler<GetChatMessagesByChatThreadIdRequest, AppResult<GetChatMessagesByChatThreadIdResponse?>>
{
    public async Task<AppResult<GetChatMessagesByChatThreadIdResponse?>> HandleAsync(
        GetChatMessagesByChatThreadIdRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new AppResult<GetChatMessagesByChatThreadIdResponse?>();

        #region # Execute

        var messages = await chatMessages.GetByChatThreadIdAsync(request.ChatThreadId, cancellationToken);

        #endregion

        #region # Handle Result

        result.Success(new GetChatMessagesByChatThreadIdResponse
        {
            Messages = ChatMessageResponse.FromEntities(messages)
        });

        #endregion

        return result;
    }
}

public sealed class GetChatMessagesByChatThreadIdResponse
{
    public List<ChatMessageResponse> Messages { get; init; } = [];
}
