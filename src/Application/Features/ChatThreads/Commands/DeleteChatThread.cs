using FluentValidation;


namespace Application.Features.ChatThreads.Commands;

public sealed record DeleteChatThreadRequest(Guid Id, Guid UserId) : ICommand;

public sealed class DeleteChatThreadRequestValidator : AbstractValidator<DeleteChatThreadRequest>
{
    public DeleteChatThreadRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Thread Id is required.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");
    }
}

public sealed class DeleteChatThreadRequestHandler(ChatThreadRepository chatThreadRepo, SharedRepository sharedRepo)
    : IRequestHandler<DeleteChatThreadRequest>
{
    private readonly SharedRepository _sharedRepo = sharedRepo;


    public async ValueTask<Result> HandleAsync(DeleteChatThreadRequest request, CancellationToken cancellationToken = default)
    {
        var result = new Result();

        #region # Execute

        var deleted = await chatThreadRepo.DeleteAsync(request.Id, request.UserId, request.UserId, cancellationToken);

        #endregion

        #region # Handle Result

        if (!deleted)
        {
            result.Failure(ErrorCode.NotFound, "Chat thread not found.");
        }
        else
        {
            result.Success();
        }

        #endregion

        return result;
    }
}
