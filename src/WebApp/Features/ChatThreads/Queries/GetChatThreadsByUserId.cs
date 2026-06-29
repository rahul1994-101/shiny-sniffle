using FluentValidation;

using WebApp.Features.Shared.Cqrs.Abstractions;

namespace WebApp.Features.ChatThreads.Queries;

public sealed record GetChatThreadsByUserIdRequest(Guid UserId)
    : IQuery<AppResult<GetChatThreadsByUserIdResponse?>>;

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
    : IFeatureHandler<GetChatThreadsByUserIdRequest, AppResult<GetChatThreadsByUserIdResponse?>>
{
    private readonly SharedRepository _sharedRepo = sharedRepo;


    public async Task<AppResult<GetChatThreadsByUserIdResponse?>> HandleAsync(GetChatThreadsByUserIdRequest request, CancellationToken cancellationToken = default)
    {
        var result = new AppResult<GetChatThreadsByUserIdResponse?>();

        #region # Execute

        var threads = await chatThreadRepo.GetActiveByUserIdAsync(request.UserId, cancellationToken);

        #endregion

        #region # Handle Result

        result.Success(new GetChatThreadsByUserIdResponse { Threads = threads });

        #endregion

        return result;
    }
}
