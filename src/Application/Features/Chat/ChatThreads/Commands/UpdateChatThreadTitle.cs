using FluentValidation;

namespace Application.Features.Chat.ChatThreads.Commands;

public sealed record UpdateChatThreadTitleRequest(Guid UserId, Guid Id, string Title)
    : ICommand<UpdateChatThreadTitleResponse>;

public sealed class UpdateChatThreadTitleResponse : ChatThreadDto
{
}

public sealed class UpdateChatThreadTitleRequestValidator : AbstractValidator<UpdateChatThreadTitleRequest>
{
    public UpdateChatThreadTitleRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");

        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Thread Id is required.");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required.")
            .Length(1, 200)
            .WithMessage("Title must be between 1 and 200 characters.");
    }
}

public sealed class UpdateChatThreadTitleRequestHandler(ChatThreadRepository chatThreadRepo)
    : IRequestHandler<UpdateChatThreadTitleRequest, UpdateChatThreadTitleResponse>
{
    public async ValueTask<Result<UpdateChatThreadTitleResponse>> HandleAsync(UpdateChatThreadTitleRequest request, CancellationToken cancellationToken = default)
    {
        var result = new Result<UpdateChatThreadTitleResponse>();

        #region # Execute

        var chatThread = await chatThreadRepo.UpdateTitleAsync(request.Id, request.UserId, request.Title, request.UserId, cancellationToken);

        #endregion

        #region # Handle Result

        if (chatThread is null)
        {
            result.Failure(ErrorCode.NotFound, "Chat thread not found.");
        }
        else
        {
            result.Success(chatThread.AsResponse<UpdateChatThreadTitleResponse>());
        }

        #endregion

        return result;
    }
}
