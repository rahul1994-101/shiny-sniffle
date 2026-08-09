using FluentValidation;

namespace Application.Features.ChatThreads.Commands;

public sealed record UpdateChatThreadAgentRequest(Guid Id, Guid UserId, ChatAgent ChatAgent)
    : ICommand<UpdateChatThreadAgentResponse>;

public sealed class UpdateChatThreadAgentResponse : ChatThreadDto
{
}

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

public sealed class UpdateChatThreadAgentRequestHandler(ChatThreadRepository chatThreadRepo, UserMailboxService mailboxService)
    : IRequestHandler<UpdateChatThreadAgentRequest, UpdateChatThreadAgentResponse>
{
    public async ValueTask<Result<UpdateChatThreadAgentResponse>> HandleAsync(UpdateChatThreadAgentRequest request, CancellationToken cancellationToken = default)
    {
        var result = new Result<UpdateChatThreadAgentResponse>();

        if (request.ChatAgent == ChatAgent.Email && !await mailboxService.IsConfiguredAsync(request.UserId, cancellationToken))
        {
            result.Failure(ErrorCode.BadRequest, "Connect your mailbox in Workspace → Email accounts before using the Email agent.");
            return result;
        }

        #region # Execute

        var chatThread = await chatThreadRepo.UpdateAgentAsync(request.Id, request.UserId, request.ChatAgent, request.UserId, cancellationToken);

        #endregion

        #region # Handle Result

        if (chatThread is null)
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
