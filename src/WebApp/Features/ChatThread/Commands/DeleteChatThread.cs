using FluentValidation;

using WebApp.Features._Shared.Abstractions;

namespace WebApp.Features.ChatThread.Commands;

public sealed record DeleteChatThreadRequest(Guid Id, Guid UserId) : ICommand<AppResult>;

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

public sealed class DeleteChatThreadRequestHandler(IChatThreadRepository chatThreads)
    : IFeatureHandler<DeleteChatThreadRequest, AppResult>
{
    public async Task<AppResult> HandleAsync(
        DeleteChatThreadRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new AppResult();

        #region # Execute

        var deleted = await chatThreads.DeleteAsync(
            request.Id,
            request.UserId,
            request.UserId,
            cancellationToken);

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
