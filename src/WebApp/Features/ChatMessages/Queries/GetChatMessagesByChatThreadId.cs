using FluentValidation;

using WebApp.Features._Shared.Abstractions;

namespace WebApp.Features.ChatMessages.Queries;

public sealed record GetChatMessagesByChatThreadIdRequest(Guid ChatThreadId)
    : IQuery<AppResult<GetChatMessagesByChatThreadIdResponse?>>;

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

public sealed class GetChatMessagesByChatThreadIdRequestHandler(ChatMessageRepository chatMessageRepo, SharedRepository sharedRepo)
    : IFeatureHandler<GetChatMessagesByChatThreadIdRequest, AppResult<GetChatMessagesByChatThreadIdResponse?>>
{
    private readonly SharedRepository _sharedRepo = sharedRepo;


    public async Task<AppResult<GetChatMessagesByChatThreadIdResponse?>> HandleAsync(GetChatMessagesByChatThreadIdRequest request, CancellationToken cancellationToken = default)
    {
        var result = new AppResult<GetChatMessagesByChatThreadIdResponse?>();

        #region # Execute

        var messages = await chatMessageRepo.GetByChatThreadIdAsync(request.ChatThreadId, cancellationToken);

        #endregion

        #region # Handle Result

        result.Success(new GetChatMessagesByChatThreadIdResponse { Messages = messages });

        #endregion

        return result;
    }
}
