using FluentValidation;

namespace Application.Features.Chat.ChatThreads.Commands;

public sealed record UpdateChatThreadAgentRequest(Guid UserId, Guid ThreadId, ChatAgent ChatAgent)
    : ICommand<UpdateChatThreadAgentResponse>;

public sealed class UpdateChatThreadAgentResponse : ChatThreadDto
{
}

public sealed class UpdateChatThreadAgentRequestValidator : AbstractValidator<UpdateChatThreadAgentRequest>
{
    public UpdateChatThreadAgentRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");

        RuleFor(x => x.ThreadId)
            .NotEmpty()
            .WithMessage("Thread Id is required.");

        RuleFor(x => x.ChatAgent)
            .IsInEnum()
            .WithMessage("Chat agent is invalid.");
    }
}

public sealed class UpdateChatThreadAgentRequestHandler(ChatThreadRepository chatThreadRepo, UserMailboxService mailboxService)
    : IRequestHandler<UpdateChatThreadAgentRequest, UpdateChatThreadAgentResponse>
{
    public async ValueTask<Result<UpdateChatThreadAgentResponse>> HandleAsync(UpdateChatThreadAgentRequest request, CancellationToken cancellationToken = default)
    {
        var result = new Result<UpdateChatThreadAgentResponse>();

        #region # Execute

        var mailboxConfigured = request.ChatAgent != ChatAgent.Email
            || await mailboxService.IsConfiguredAsync(request.UserId, cancellationToken);
        ChatThreadDto? chatThread = null;
        if (mailboxConfigured)
        {
            chatThread = await chatThreadRepo.UpdateAgentAsync(request.UserId, request.ThreadId, request.ChatAgent, request.UserId, cancellationToken);
        }

        #endregion

        #region # Handle Result

        if (!mailboxConfigured)
        {
            result.Failure(ErrorCode.BadRequest, "Connect your mailbox in Workspace → Email accounts before using the Email agent.");
        }
        else if (chatThread is null)
        {
            result.Failure(ErrorCode.NotFound, "Chat thread not found.");
        }
        else
        {
            result.Success(chatThread.AsResponse<UpdateChatThreadAgentResponse>());
        }

        #endregion

        return result;
    }
}
