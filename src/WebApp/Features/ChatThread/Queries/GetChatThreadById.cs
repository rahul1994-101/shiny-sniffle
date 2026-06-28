using FluentValidation;

using WebApp.Features._Shared.Abstractions;
using WebApp.Features.ChatThread;

namespace WebApp.Features.ChatThread.Queries;

public sealed record GetChatThreadByIdRequest(Guid Id)
    : IQuery<AppResult<ChatThreadResponse?>>;

public sealed class GetChatThreadByIdRequestValidator : AbstractValidator<GetChatThreadByIdRequest>
{
    public GetChatThreadByIdRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Thread Id is required.");
    }
}

public sealed class GetChatThreadByIdRequestHandler(IChatThreadRepository chatThreads)
    : IFeatureHandler<GetChatThreadByIdRequest, AppResult<ChatThreadResponse?>>
{
    public async Task<AppResult<ChatThreadResponse?>> HandleAsync(
        GetChatThreadByIdRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new AppResult<ChatThreadResponse?>();

        #region # Execute

        var chatThread = await chatThreads.GetActiveByIdAsync(request.Id, cancellationToken);

        #endregion

        #region # Handle Result

        if (chatThread is null)
        {
            result.Failure(ErrorCode.NotFound, "Chat thread not found.");
        }
        else
        {
            result.Success(ChatThreadResponse.FromEntity(chatThread));
        }

        #endregion

        return result;
    }
}
