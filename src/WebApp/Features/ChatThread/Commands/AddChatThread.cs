using FluentValidation;

using WebApp.Features._Shared.Abstractions;

namespace WebApp.Features.ChatThread.Commands;

public sealed record AddChatThreadCommand(AddChatThreadRequest Request) : ICommand<AppResult<ChatThreadDto?>>;

public sealed class AddChatThreadCommandValidator : AbstractValidator<AddChatThreadCommand>
{
    public AddChatThreadCommandValidator()
    {
        RuleFor(x => x.Request)
            .NotNull()
            .WithMessage("Request can't be empty.");

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

public sealed class AddChatThreadCommandHandler(IChatThreadRepository chatThreads)
    : IFeatureHandler<AddChatThreadCommand, AppResult<ChatThreadDto?>>
{
    public async Task<AppResult<ChatThreadDto?>> HandleAsync(
        AddChatThreadCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = new AppResult<ChatThreadDto?>();

        #region # Execute

        var chatThread = await chatThreads.AddChatThreadAsync(command.Request);

        #endregion

        #region # Handle Result

        if (chatThread is null)
        {
            result.Failure(ErrorCode.InternalServerError, "Failed to create chat thread.");
        }
        else
        {
            result.Success(chatThread);
        }

        #endregion

        return result;
    }
}
