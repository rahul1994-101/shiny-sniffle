using FluentValidation;

using WebApp.Features._Shared.Abstractions;

namespace WebApp.Features.ChatThread.Commands;

public sealed record UpdateChatThreadTitleCommand(UpdateChatThreadTitleRequest Request)
    : ICommand<AppResult<ChatThreadDto?>>;

public sealed class UpdateChatThreadTitleCommandValidator : AbstractValidator<UpdateChatThreadTitleCommand>
{
    public UpdateChatThreadTitleCommandValidator()
    {
        RuleFor(x => x.Request)
            .NotNull()
            .WithMessage("Request can't be empty.");

        RuleFor(x => x.Request.Id)
            .NotEmpty()
            .WithMessage("Thread Id is required.");

        RuleFor(x => x.Request.Title)
            .NotEmpty()
            .WithMessage("Title is required.")
            .Length(1, 200)
            .WithMessage("Title must be between 1 and 200 characters.");

        RuleFor(x => x.Request.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");
    }
}

public sealed class UpdateChatThreadTitleCommandHandler(IChatThreadRepository chatThreads)
    : IFeatureHandler<UpdateChatThreadTitleCommand, AppResult<ChatThreadDto?>>
{
    public async Task<AppResult<ChatThreadDto?>> HandleAsync(
        UpdateChatThreadTitleCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = new AppResult<ChatThreadDto?>();

        #region # Execute

        var chatThread = await chatThreads.UpdateChatThreadTitleAsync(command.Request);

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
