using FluentValidation;
using Application.Features.chat.ChatMessages;

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

public sealed class GetChatMessagesByChatThreadIdRequestHandler(ChatMessageRepository chatMessageRepo)
    : IRequestHandler<GetChatMessagesByChatThreadIdRequest, GetChatMessagesByChatThreadIdResponse>
{
    public async ValueTask<Result<GetChatMessagesByChatThreadIdResponse>> HandleAsync(GetChatMessagesByChatThreadIdRequest request, CancellationToken cancellationToken = default)
    {
        var result = new Result<GetChatMessagesByChatThreadIdResponse>();

        #region # Execute

        var messages = await chatMessageRepo.GetByChatThreadIdAsync(request.ChatThreadId, cancellationToken);

        #endregion

        #region # Handle Result

        result.Success(new GetChatMessagesByChatThreadIdResponse { Messages = messages });

        #endregion

        return result;
    }
}
