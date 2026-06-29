using FluentValidation;

using WebApp.Features.Shared.Cqrs.Abstractions;

namespace WebApp.Features.ChatThreads.Queries;

public sealed record GetChatThreadByIdRequest(Guid Id)
    : IQuery<AppResult<GetChatThreadByIdResponse?>>;

public sealed class GetChatThreadByIdResponse : ChatThreadDto
{
}

public sealed class GetChatThreadByIdRequestValidator : AbstractValidator<GetChatThreadByIdRequest>
{
    public GetChatThreadByIdRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Thread Id is required.");
    }
}

public sealed class GetChatThreadByIdRequestHandler(ChatThreadRepository chatThreadRepo, SharedRepository sharedRepo)
    : IFeatureHandler<GetChatThreadByIdRequest, AppResult<GetChatThreadByIdResponse?>>
{
    private readonly SharedRepository _sharedRepo = sharedRepo;


    public async Task<AppResult<GetChatThreadByIdResponse?>> HandleAsync(GetChatThreadByIdRequest request, CancellationToken cancellationToken = default)
    {
        var result = new AppResult<GetChatThreadByIdResponse?>();

        #region # Execute

        var chatThread = await chatThreadRepo.GetActiveByIdAsync(request.Id, cancellationToken);

        #endregion

        #region # Handle Result

        if (chatThread is null)
        {
            result.Failure(ErrorCode.NotFound, "Chat thread not found.");
        }
        else
        {
            result.Success(chatThread.AsResponse<GetChatThreadByIdResponse>());
        }

        #endregion

        return result;
    }
}
