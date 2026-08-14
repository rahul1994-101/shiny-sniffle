using FluentValidation;
using Application.Features.Chat.ChatMessages;

namespace Application.Features.Chat.ChatMessages.Queries;

public sealed record GetChatMessagesByChatThreadIdRequest(Guid UserId, Guid ChatThreadId)
    : IQuery<GetChatMessagesByChatThreadIdResponse>;

public sealed class GetChatMessagesByChatThreadIdResponse
{
    public List<ChatMessageDto> Messages { get; init; } = [];
}

public sealed class GetChatMessagesByChatThreadIdRequestValidator : AbstractValidator<GetChatMessagesByChatThreadIdRequest>
{
    public GetChatMessagesByChatThreadIdRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");

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

        var messages = await chatMessageRepo.GetByChatThreadIdAsync(request.UserId, request.ChatThreadId, cancellationToken);

        #endregion

        #region # Handle Result

        if (messages is null)
        {
            result.Failure(ErrorCode.NotFound, "Chat thread not found.");
        }
        else
        {
            result.Success(new GetChatMessagesByChatThreadIdResponse { Messages = messages });
        }

        #endregion

        return result;
    }
}
