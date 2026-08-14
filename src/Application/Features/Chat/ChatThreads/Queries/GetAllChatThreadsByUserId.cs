using FluentValidation;

namespace Application.Features.Chat.ChatThreads.Queries;

public sealed record GetAllChatThreadsByUserIdRequest(Guid UserId)
    : IQuery<GetAllChatThreadsByUserIdResponse>;

public sealed class GetAllChatThreadsByUserIdResponse
{
    public List<ChatThreadDto> Threads { get; init; } = [];
}

public sealed class GetAllChatThreadsByUserIdRequestValidator : AbstractValidator<GetAllChatThreadsByUserIdRequest>
{
    public GetAllChatThreadsByUserIdRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");
    }
}

public sealed class GetAllChatThreadsByUserIdRequestHandler(ChatThreadRepository chatThreadRepo)
    : IRequestHandler<GetAllChatThreadsByUserIdRequest, GetAllChatThreadsByUserIdResponse>
{
    public async ValueTask<Result<GetAllChatThreadsByUserIdResponse>> HandleAsync(GetAllChatThreadsByUserIdRequest request, CancellationToken cancellationToken = default)
    {
        var result = new Result<GetAllChatThreadsByUserIdResponse>();

        #region # Execute

        var threads = await chatThreadRepo.GetAllChatThreadsByUserIdAsync(request.UserId, cancellationToken);

        #endregion

        #region # Handle Result

        result.Success(new GetAllChatThreadsByUserIdResponse { Threads = threads });

        #endregion

        return result;
    }
}
