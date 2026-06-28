using FluentValidation;

using WebApp.Features._Shared.Abstractions;

namespace WebApp.Features.ChatThread.Queries;

public sealed record GetChatThreadByIdQuery(Guid Id) : IQuery<AppResult<ChatThreadDto?>>;

public sealed class GetChatThreadByIdQueryValidator : AbstractValidator<GetChatThreadByIdQuery>
{
    public GetChatThreadByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Thread Id is required.");
    }
}

public sealed class GetChatThreadByIdQueryHandler(IChatThreadRepository chatThreads)
    : IFeatureHandler<GetChatThreadByIdQuery, AppResult<ChatThreadDto?>>
{
    public async Task<AppResult<ChatThreadDto?>> HandleAsync(
        GetChatThreadByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = new AppResult<ChatThreadDto?>();

        #region # Execute

        var chatThread = await chatThreads.GetChatThreadByIdAsync(query.Id);

        #endregion

        #region # Handle Result

        if (chatThread is null)
        {
            result.Failure(ErrorCode.NotFound, "Chat thread not found.");
        }
        else
        {
            result.Success(chatThread);
        }

        #endregion

        return result;
    }
}
