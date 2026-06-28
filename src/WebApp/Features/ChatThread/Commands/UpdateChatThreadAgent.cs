using FluentValidation;

using WebApp.Features._Shared.Abstractions;
using WebApp.Utilities.Services;

namespace WebApp.Features.ChatThread.Commands;

public sealed record UpdateChatThreadAgentRequest(Guid Id, Guid UserId, ChatAgent ChatAgent)
    : ICommand<AppResult<ChatThreadResponse?>>;

public sealed class UpdateChatThreadAgentRequestValidator : AbstractValidator<UpdateChatThreadAgentRequest>
{
    public UpdateChatThreadAgentRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Thread Id is required.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");
    }
}

public sealed class UpdateChatThreadAgentRequestHandler(
    IChatThreadRepository chatThreads,
    UserMailboxService mailboxService)
    : IFeatureHandler<UpdateChatThreadAgentRequest, AppResult<ChatThreadResponse?>>
{
    public async Task<AppResult<ChatThreadResponse?>> HandleAsync(
        UpdateChatThreadAgentRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new AppResult<ChatThreadResponse?>();

        if (request.ChatAgent == ChatAgent.Email
            && !await mailboxService.IsConfiguredAsync(request.UserId, cancellationToken))
        {
            result.Failure(
                ErrorCode.BadRequest,
                "Connect your mailbox in Settings → Email before using the Email agent.");
            return result;
        }

        #region # Execute

        var chatThread = await chatThreads.UpdateAgentAsync(
            request.Id,
            request.UserId,
            request.ChatAgent,
            request.UserId,
            cancellationToken);

        #endregion

        #region # Handle Result

        if (chatThread is null)
        {
            result.Failure(ErrorCode.NotFound, "Chat thread not found.");
        }
        else
        {
            result.Success(ChatThreadResponse.FromEntity(chatThread));
        }

        #endregion

        return result;
    }
}
