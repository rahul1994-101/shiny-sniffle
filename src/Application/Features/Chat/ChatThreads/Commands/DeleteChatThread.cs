using FluentValidation;

namespace Application.Features.Chat.ChatThreads.Commands;

public sealed record DeleteChatThreadRequest(Guid UserId, Guid ThreadId) : ICommand<DeleteChatThreadResponse>;

public sealed class DeleteChatThreadResponse;

public sealed class DeleteChatThreadRequestValidator : AbstractValidator<DeleteChatThreadRequest>
{
    public DeleteChatThreadRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");

        RuleFor(x => x.ThreadId)
            .NotEmpty()
            .WithMessage("Thread Id is required.");
    }
}

public sealed class DeleteChatThreadRequestHandler(ChatThreadRepository chatThreadRepo)
    : IRequestHandler<DeleteChatThreadRequest, DeleteChatThreadResponse>
{
    public async ValueTask<Result<DeleteChatThreadResponse>> HandleAsync(DeleteChatThreadRequest request, CancellationToken cancellationToken = default)
    {
        var result = new Result<DeleteChatThreadResponse>();

        #region # Execute

        var deleted = await chatThreadRepo.DeleteAsync(request.UserId, request.ThreadId, request.UserId, cancellationToken);

        #endregion

        #region # Handle Result

        if (!deleted)
        {
            result.Failure(ErrorCode.NotFound, "Chat thread not found.");
        }
        else
        {
            result.Success(new DeleteChatThreadResponse());
        }

        #endregion

        return result;
    }
}
