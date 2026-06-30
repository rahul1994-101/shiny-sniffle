using FluentValidation;


namespace WebApp.Features.ChatThreads.Queries;

public sealed record GetChatThreadsByUserIdRequest(Guid UserId)
    : IQuery<GetChatThreadsByUserIdResponse?>;

public sealed class GetChatThreadsByUserIdResponse
{
    public List<ChatThreadDto> Threads { get; init; } = [];
}

public sealed class GetChatThreadsByUserIdRequestValidator : AbstractValidator<GetChatThreadsByUserIdRequest>
{
    public GetChatThreadsByUserIdRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");
    }
}

public sealed class GetChatThreadsByUserIdRequestHandler(ChatThreadRepository chatThreadRepo, SharedRepository sharedRepo)
    : IRequestHandler<GetChatThreadsByUserIdRequest, Result<GetChatThreadsByUserIdResponse?>>
{
    private readonly SharedRepository _sharedRepo = sharedRepo;


    public async ValueTask<Result<GetChatThreadsByUserIdResponse?>> HandleAsync(GetChatThreadsByUserIdRequest request, CancellationToken cancellationToken = default)
    {
        var result = new Result<GetChatThreadsByUserIdResponse?>();

        #region # Execute

        var threads = await chatThreadRepo.GetActiveByUserIdAsync(request.UserId, cancellationToken);

        #endregion

        #region # Handle Result

        result.Success(new GetChatThreadsByUserIdResponse { Threads = threads });

        #endregion

        return result;
    }
}
