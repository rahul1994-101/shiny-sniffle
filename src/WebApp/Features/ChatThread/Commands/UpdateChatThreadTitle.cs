using FluentValidation;

using WebApp.Features._Shared.Abstractions;
using WebApp.Features.ChatThread;

namespace WebApp.Features.ChatThread.Commands;

public sealed record UpdateChatThreadTitleRequest(Guid Id, string Title, Guid UserId)
    : ICommand<AppResult<ChatThreadResponse?>>;

public sealed class UpdateChatThreadTitleRequestValidator : AbstractValidator<UpdateChatThreadTitleRequest>
{
    public UpdateChatThreadTitleRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Thread Id is required.");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required.")
            .Length(1, 200)
            .WithMessage("Title must be between 1 and 200 characters.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");
    }
}

public sealed class UpdateChatThreadTitleRequestHandler(IChatThreadRepository chatThreads)
    : IFeatureHandler<UpdateChatThreadTitleRequest, AppResult<ChatThreadResponse?>>
{
    public async Task<AppResult<ChatThreadResponse?>> HandleAsync(
        UpdateChatThreadTitleRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new AppResult<ChatThreadResponse?>();

        #region # Execute

        var chatThread = await chatThreads.UpdateTitleAsync(
            request.Id,
            request.UserId,
            request.Title,
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
