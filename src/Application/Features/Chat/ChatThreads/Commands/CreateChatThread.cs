using FluentValidation;

namespace Application.Features.Chat.ChatThreads.Commands;

public sealed record CreateChatThreadRequest(Guid UserId, string Title, ChatAgent ChatAgent = default)
    : ICommand<CreateChatThreadResponse>;

public sealed class CreateChatThreadResponse : ChatThreadDto
{
}

public sealed class CreateChatThreadRequestValidator : AbstractValidator<CreateChatThreadRequest>
{
    public CreateChatThreadRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required.")
            .Length(1, 200)
            .WithMessage("Title must be between 1 and 200 characters.");

        RuleFor(x => x.ChatAgent)
            .IsInEnum()
            .WithMessage("Chat agent is invalid.");
    }
}

public sealed class CreateChatThreadRequestHandler(ChatThreadRepository chatThreadRepo, WorkspaceReferenceService workspaceRefs)
    : IRequestHandler<CreateChatThreadRequest, CreateChatThreadResponse>
{
    public async ValueTask<Result<CreateChatThreadResponse>> HandleAsync(CreateChatThreadRequest request, CancellationToken cancellationToken = default)
    {
        var result = new Result<CreateChatThreadResponse>();

        #region # Execute

        var mailboxConfigured = request.ChatAgent != ChatAgent.Email
            || await workspaceRefs.IsMailboxConfiguredAsync(request.UserId, cancellationToken: cancellationToken);
        ChatThreadDto? chatThread = null;
        if (mailboxConfigured)
        {
            chatThread = await chatThreadRepo.AddAsync(new ChatThread
            {
                Title = request.Title,
                UserId = request.UserId,
                ChatAgent = ChatAgentHelpers.ToPersistence(request.ChatAgent)
            }, cancellationToken);
        }

        #endregion

        #region # Handle Result

        if (!mailboxConfigured)
        {
            result.Failure(ErrorCode.BadRequest, "Connect your mailbox in Workspace → Email accounts before using the Email agent.");
        }
        else
        {
            result.Success(chatThread!.AsResponse<CreateChatThreadResponse>());
        }

        #endregion

        return result;
    }
}
