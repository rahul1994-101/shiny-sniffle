using FluentValidation;

using WebApp.AI;
using WebApp.Features.Shared.Cqrs.Abstractions;
using WebApp.Features.ChatMessages;
using WebApp.Features.ChatThreads;

namespace WebApp.Features.ChatMessages.Commands;

public sealed record SendChatMessageRequest(Guid ChatThreadId, Guid UserId, ChatAgent ChatAgent, string Message)
    : ICommand<AppResult<SendChatMessageResponse?>>;

public sealed class SendChatMessageResponse
{
    public ChatMessageDto UserMessage { get; init; } = null!;
    public ChatMessageDto AssistantMessage { get; init; } = null!;
}

public sealed class SendChatMessageRequestValidator : AbstractValidator<SendChatMessageRequest>
{
    public SendChatMessageRequestValidator()
    {
        RuleFor(x => x.ChatThreadId)
            .NotEmpty()
            .WithMessage("Chat Thread Id is required.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User Id is required.");

        RuleFor(x => x.Message)
            .Must(message => !string.IsNullOrWhiteSpace(message))
            .WithMessage("Message is required.");
    }
}

public sealed class SendChatMessageRequestHandler(ChatThreadRepository chatThreadRepo, ChatMessageRepository chatMessageRepo, SharedRepository sharedRepo, ChatOrchestrator chatOrchestrator)
    : IFeatureHandler<SendChatMessageRequest, AppResult<SendChatMessageResponse?>>
{
    private readonly SharedRepository _sharedRepo = sharedRepo;


    public async Task<AppResult<SendChatMessageResponse?>> HandleAsync(SendChatMessageRequest request, CancellationToken cancellationToken = default)
    {
        var result = new AppResult<SendChatMessageResponse?>();
        var text = request.Message.Trim();

        var thread = await chatThreadRepo.GetActiveByIdAsync(request.ChatThreadId, cancellationToken);
        if (thread is null || thread.UserId != request.UserId)
        {
            result.Failure(ErrorCode.NotFound, "Chat thread not found.");
            return result;
        }

        var chatAgent = request.ChatAgent == thread.ChatAgent ? request.ChatAgent : thread.ChatAgent;

        #region # Execute

        var userMessage = await chatMessageRepo.AddAsync(new ChatMessage
        {
            ChatThreadId = request.ChatThreadId,
            Role = ChatMessageRoles.User,
            Content = text,
            CreatedBy = request.UserId,
            UpdatedBy = request.UserId
        }, cancellationToken);
        if (userMessage is null)
        {
            result.Failure(ErrorCode.InternalServerError, "Failed to create chat message.");
            return result;
        }

        var agentRun = await chatOrchestrator.RunChatAgentAsync(new RunChatAgentRequest
        {
            ChatThreadId = request.ChatThreadId,
            UserId = request.UserId,
            ChatAgent = chatAgent
        });

        var assistantMessage = await chatMessageRepo.AddAsync(new ChatMessage
        {
            ChatThreadId = request.ChatThreadId,
            Role = ChatMessageRoles.Assistant,
            Content = agentRun.AssistantContent,
            CreatedBy = request.UserId,
            UpdatedBy = request.UserId
        }, cancellationToken);
        if (assistantMessage is null)
        {
            result.Failure(ErrorCode.InternalServerError, "Failed to create chat message.");
            return result;
        }

        #endregion

        #region # Handle Result

        result.Success(new SendChatMessageResponse { UserMessage = userMessage, AssistantMessage = assistantMessage });

        #endregion

        return result;
    }
}
