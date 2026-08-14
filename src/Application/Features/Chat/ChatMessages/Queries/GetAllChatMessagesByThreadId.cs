using FluentValidation;
using Application.Features.Chat.ChatMessages;

namespace Application.Features.Chat.ChatMessages.Queries;

public sealed record GetAllChatMessagesByThreadIdRequest(Guid UserId, Guid ThreadId)
    : IQuery<GetAllChatMessagesByThreadIdResponse>;

public sealed class GetAllChatMessagesByThreadIdResponse
{
    public List<ChatMessageDto> Messages { get; init; } = [];
}

public sealed class GetAllChatMessagesByThreadIdRequestValidator : AbstractValidator<GetAllChatMessagesByThreadIdRequest>
{
    public GetAllChatMessagesByThreadIdRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");

        RuleFor(x => x.ThreadId)
            .NotEmpty()
            .WithMessage("Thread Id is required.");
    }
}

public sealed class GetAllChatMessagesByThreadIdRequestHandler(ChatMessageRepository chatMessageRepo)
    : IRequestHandler<GetAllChatMessagesByThreadIdRequest, GetAllChatMessagesByThreadIdResponse>
{
    public async ValueTask<Result<GetAllChatMessagesByThreadIdResponse>> HandleAsync(GetAllChatMessagesByThreadIdRequest request, CancellationToken cancellationToken = default)
    {
        var result = new Result<GetAllChatMessagesByThreadIdResponse>();

        #region # Execute

        var messages = await chatMessageRepo.GetAllChatMessagesByThreadIdAsync(request.UserId, request.ThreadId, cancellationToken);

        #endregion

        #region # Handle Result

        if (messages is null)
        {
            result.Failure(ErrorCode.NotFound, "Chat thread not found.");
        }
        else
        {
            result.Success(new GetAllChatMessagesByThreadIdResponse { Messages = messages });
        }

        #endregion

        return result;
    }
}
