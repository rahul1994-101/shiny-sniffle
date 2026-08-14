using FluentValidation;

namespace Application.Features.Chat.ChatThreads.Commands;

public sealed record AddChatThreadRequest(Guid UserId, string Title, ChatAgent ChatAgent = default)
    : ICommand<AddChatThreadResponse>;

public sealed class AddChatThreadResponse : ChatThreadDto
{
}

public sealed class AddChatThreadRequestValidator : AbstractValidator<AddChatThreadRequest>
{
    public AddChatThreadRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required.")
            .Length(1, 200)
            .WithMessage("Title must be between 1 and 200 characters.");
    }
}

public sealed class AddChatThreadRequestHandler(ChatThreadRepository chatThreadRepo, UserMailboxService mailboxService)
    : IRequestHandler<AddChatThreadRequest, AddChatThreadResponse>
{
    public async ValueTask<Result<AddChatThreadResponse>> HandleAsync(AddChatThreadRequest request, CancellationToken cancellationToken = default)
    {
        var result = new Result<AddChatThreadResponse>();

        #region # Execute

        var mailboxConfigured = request.ChatAgent != ChatAgent.Email
            || await mailboxService.IsConfiguredAsync(request.UserId, cancellationToken);
        ChatThreadDto? chatThread = null;
        if (mailboxConfigured)
        {
            chatThread = await chatThreadRepo.AddAsync(new ChatThread
            {
                Title = request.Title,
                UserId = request.UserId,
                ChatAgent = ChatAgentHelpers.ToPersistence(request.ChatAgent),
                CreatedBy = request.UserId,
                UpdatedBy = request.UserId
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
            result.Success(chatThread!.AsResponse<AddChatThreadResponse>());
        }

        #endregion

        return result;
    }
}
