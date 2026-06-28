using FluentValidation;

using WebApp.Features._Shared.Abstractions;

namespace WebApp.Features.ChatThread.Commands;

public sealed record UpdateChatThreadTitleRequest(Guid Id, string Title, Guid UserId)
    : ICommand<AppResult<UpdateChatThreadTitleResponse?>>;

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
    : IFeatureHandler<UpdateChatThreadTitleRequest, AppResult<UpdateChatThreadTitleResponse?>>
{
    public async Task<AppResult<UpdateChatThreadTitleResponse?>> HandleAsync(
        UpdateChatThreadTitleRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new AppResult<UpdateChatThreadTitleResponse?>();

        #region # Execute

        var chatThread = await chatThreads.UpdateChatThreadTitleAsync(new Core.DTOs.UpdateChatThreadTitleRequest
        {
            Id = request.Id,
            Title = request.Title,
            UserId = request.UserId
        });

        #endregion

        #region # Handle Result

        if (chatThread is null)
        {
            result.Failure(ErrorCode.NotFound, "Chat thread not found.");
        }
        else
        {
            result.Success(ToResponse(chatThread));
        }

        #endregion

        return result;
    }

    #region # Mapping

    private static UpdateChatThreadTitleResponse ToResponse(ChatThreadDto thread) => new()
    {
        Id = thread.Id,
        Title = thread.Title,
        UserId = thread.UserId,
        ChatAgent = thread.ChatAgent,
        CreatedAt = thread.CreatedAt,
        UpdatedAt = thread.UpdatedAt
    };

    #endregion
}

public sealed class UpdateChatThreadTitleResponse
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public ChatAgent ChatAgent { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
