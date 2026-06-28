using FluentValidation;

using WebApp.Features._Shared.Abstractions;

namespace WebApp.Features.ChatThread.Queries;

public sealed record GetChatThreadByIdRequest(Guid Id)
    : IQuery<AppResult<GetChatThreadByIdResponse?>>;

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
    : IFeatureHandler<GetChatThreadByIdRequest, AppResult<GetChatThreadByIdResponse?>>
{
    public async Task<AppResult<GetChatThreadByIdResponse?>> HandleAsync(
        GetChatThreadByIdRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new AppResult<GetChatThreadByIdResponse?>();

        #region # Execute

        var chatThread = await chatThreads.GetChatThreadByIdAsync(request.Id);

        #endregion

        #region # Handle Result

        if (chatThread is null)
        {
            result.Failure(ErrorCode.NotFound, "Chat thread not found.");
        }
        else
        {
            result.Success(ToResponse(chatThread));
        }

        #endregion

        return result;
    }

    #region # Mapping

    private static GetChatThreadByIdResponse ToResponse(ChatThreadDto thread) => new()
    {
        Id = thread.Id,
        Title = thread.Title,
        UserId = thread.UserId,
        ChatAgent = thread.ChatAgent,
        CreatedAt = thread.CreatedAt,
        UpdatedAt = thread.UpdatedAt
    };

    #endregion
}

public sealed class GetChatThreadByIdResponse
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public ChatAgent ChatAgent { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
