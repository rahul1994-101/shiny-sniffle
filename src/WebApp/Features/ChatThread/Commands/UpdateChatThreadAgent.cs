using FluentValidation;

using WebApp.Features._Shared.Abstractions;
using WebApp.Utilities.Services;

namespace WebApp.Features.ChatThread.Commands;

public sealed record UpdateChatThreadAgentCommand(UpdateChatThreadAgentRequest Request)
    : ICommand<AppResult<ChatThreadDto?>>;

public sealed class UpdateChatThreadAgentCommandValidator : AbstractValidator<UpdateChatThreadAgentCommand>
{
    public UpdateChatThreadAgentCommandValidator()
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

public sealed class UpdateChatThreadAgentCommandHandler(
    IChatThreadRepository chatThreads,
    UserMailboxService mailboxService)
    : IFeatureHandler<UpdateChatThreadAgentCommand, AppResult<ChatThreadDto?>>
{
    public async Task<AppResult<ChatThreadDto?>> HandleAsync(
        UpdateChatThreadAgentCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = new AppResult<ChatThreadDto?>();
        var request = command.Request;

        if (request.ChatAgent == ChatAgent.Email
            && !await mailboxService.IsConfiguredAsync(request.UserId, cancellationToken))
        {
            result.Failure(
                ErrorCode.BadRequest,
                "Connect your mailbox in Settings → Email before using the Email agent.");
            return result;
        }

        #region # Execute

        var chatThread = await chatThreads.UpdateChatThreadAgentAsync(request);

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
