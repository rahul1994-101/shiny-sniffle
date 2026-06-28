using FluentValidation;

using WebApp.Features._Shared.Abstractions;

namespace WebApp.Features.ChatThread.Queries;

public sealed record GetChatThreadsByUserIdRequest(Guid UserId)
    : IQuery<AppResult<GetChatThreadsByUserIdResponse?>>;

public sealed class GetChatThreadsByUserIdRequestValidator : AbstractValidator<GetChatThreadsByUserIdRequest>
{
    public GetChatThreadsByUserIdRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");
    }
}

public sealed class GetChatThreadsByUserIdRequestHandler(IChatThreadRepository chatThreads)
    : IFeatureHandler<GetChatThreadsByUserIdRequest, AppResult<GetChatThreadsByUserIdResponse?>>
{
    public async Task<AppResult<GetChatThreadsByUserIdResponse?>> HandleAsync(
        GetChatThreadsByUserIdRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new AppResult<GetChatThreadsByUserIdResponse?>();

        #region # Execute

        var threads = await chatThreads.GetActiveByUserIdAsync(request.UserId, cancellationToken);

        #endregion

        #region # Handle Result

        result.Success(new GetChatThreadsByUserIdResponse
        {
            Threads = ChatThreadResponse.FromEntities(threads)
        });

        #endregion

        return result;
    }
}

public sealed class GetChatThreadsByUserIdResponse
{
    public List<ChatThreadResponse> Threads { get; init; } = [];
}
