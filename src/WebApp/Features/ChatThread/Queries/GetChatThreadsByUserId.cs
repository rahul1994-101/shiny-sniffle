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

        var threads = await chatThreads.GetChatThreadsByUserIdAsync(request.UserId);

        #endregion

        #region # Handle Result

        if (threads is null)
        {
            result.Failure(ErrorCode.InternalServerError, "Failed to fetch chat threads.");
        }
        else
        {
            result.Success(new GetChatThreadsByUserIdResponse { Threads = threads });
        }

        #endregion

        return result;
    }
}

public sealed class GetChatThreadsByUserIdResponse
{
    public List<ChatThreadDto> Threads { get; init; } = [];
}
