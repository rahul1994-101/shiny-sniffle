using FluentValidation;

using WebApp.Features._Shared.Abstractions;

namespace WebApp.Features.ChatThread.Queries;

public sealed record GetChatThreadsByUserIdQuery(Guid UserId) : IQuery<AppResult<List<ChatThreadDto>?>>;

public sealed class GetChatThreadsByUserIdQueryValidator : AbstractValidator<GetChatThreadsByUserIdQuery>
{
    public GetChatThreadsByUserIdQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");
    }
}

public sealed class GetChatThreadsByUserIdQueryHandler(IChatThreadRepository chatThreads)
    : IFeatureHandler<GetChatThreadsByUserIdQuery, AppResult<List<ChatThreadDto>?>>
{
    public async Task<AppResult<List<ChatThreadDto>?>> HandleAsync(
        GetChatThreadsByUserIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = new AppResult<List<ChatThreadDto>?>();

        #region # Execute

        var threads = await chatThreads.GetChatThreadsByUserIdAsync(query.UserId);

        #endregion

        #region # Handle Result

        if (threads is null)
        {
            result.Failure(ErrorCode.InternalServerError, "Failed to fetch chat threads.");
        }
        else
        {
            result.Success(threads);
        }

        #endregion

        return result;
    }
}
