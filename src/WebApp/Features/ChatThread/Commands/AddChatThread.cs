using FluentValidation;

using WebApp.Features._Shared.Abstractions;

namespace WebApp.Features.ChatThread.Commands;

public sealed record AddChatThreadRequest(string Title, Guid UserId, ChatAgent ChatAgent = default)
    : ICommand<AppResult<AddChatThreadResponse?>>;

public sealed class AddChatThreadRequestValidator : AbstractValidator<AddChatThreadRequest>
{
    public AddChatThreadRequestValidator()
    {
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

public sealed class AddChatThreadRequestHandler(IChatThreadRepository chatThreads)
    : IFeatureHandler<AddChatThreadRequest, AppResult<AddChatThreadResponse?>>
{
    public async Task<AppResult<AddChatThreadResponse?>> HandleAsync(
        AddChatThreadRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new AppResult<AddChatThreadResponse?>();

        #region # Execute

        var chatThread = await chatThreads.AddChatThreadAsync(new Core.DTOs.AddChatThreadRequest
        {
            Title = request.Title,
            UserId = request.UserId,
            ChatAgent = request.ChatAgent
        });

        #endregion

        #region # Handle Result

        if (chatThread is null)
        {
            result.Failure(ErrorCode.InternalServerError, "Failed to create chat thread.");
        }
        else
        {
            result.Success(ToResponse(chatThread));
        }

        #endregion

        return result;
    }

    #region # Mapping

    private static AddChatThreadResponse ToResponse(ChatThreadDto thread) => new()
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

public sealed class AddChatThreadResponse
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public ChatAgent ChatAgent { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
