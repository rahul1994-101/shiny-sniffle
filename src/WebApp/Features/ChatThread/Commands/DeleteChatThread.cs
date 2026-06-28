using FluentValidation;

using WebApp.Features._Shared.Abstractions;

namespace WebApp.Features.ChatThread.Commands;

public sealed record DeleteChatThreadCommand(DeleteChatThreadRequest Request) : ICommand<AppResult>;

public sealed class DeleteChatThreadCommandValidator : AbstractValidator<DeleteChatThreadCommand>
{
    public DeleteChatThreadCommandValidator()
    {
        RuleFor(x => x.Request)
            .NotNull()
            .WithMessage("Request can't be empty.");

        RuleFor(x => x.Request.Id)
            .NotEmpty()
            .WithMessage("Thread Id is required.");

        RuleFor(x => x.Request.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");
    }
}

public sealed class DeleteChatThreadCommandHandler(IChatThreadRepository chatThreads)
    : IFeatureHandler<DeleteChatThreadCommand, AppResult>
{
    public async Task<AppResult> HandleAsync(DeleteChatThreadCommand command, CancellationToken cancellationToken = default)
    {
        var result = new AppResult();

        #region # Execute

        var deleted = await chatThreads.DeleteChatThreadAsync(command.Request);

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
