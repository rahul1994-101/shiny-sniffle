using FluentValidation;

using Application.AI;
using Application.AI.Memory;
using Application.Features.ChatMessages;
using Application.Features.ChatThreads;

namespace Application.Features.ChatMessages.Commands;

public sealed record SendChatMessageRequest(Guid ChatThreadId, Guid UserId, ChatAgent ChatAgent, string Message)
    : ICommand<SendChatMessageResponse>;

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

public sealed class SendChatMessageRequestHandler(
    ChatThreadRepository chatThreadRepo,
    ChatMessageRepository chatMessageRepo,
    SharedRepository sharedRepo,
    ChatOrchestrator chatOrchestrator,
    ThreadMemoryService threadMemory)
    : IRequestHandler<SendChatMessageRequest, SendChatMessageResponse>
{
    private readonly SharedRepository _sharedRepo = sharedRepo;


    public async ValueTask<Result<SendChatMessageResponse>> HandleAsync(SendChatMessageRequest request, CancellationToken cancellationToken = default)
    {
        var result = new Result<SendChatMessageResponse>();
        var text = request.Message.Trim();

        #region # Execute

        var thread = await chatThreadRepo.GetActiveByIdAsync(request.ChatThreadId, cancellationToken);
        ChatMessageDto? userMessage = null;
        ChatMessageDto? assistantMessage = null;

        if (thread is not null && thread.UserId == request.UserId)
        {
            var chatAgent = request.ChatAgent == thread.ChatAgent ? request.ChatAgent : thread.ChatAgent;

            userMessage = await chatMessageRepo.AddAsync(new ChatMessage
            {
                ChatThreadId = request.ChatThreadId,
                Role = ChatMessageRoles.User,
                Content = text,
                CreatedBy = request.UserId,
                UpdatedBy = request.UserId
            }, cancellationToken);

            if (userMessage is not null)
            {
                var agentRun = await chatOrchestrator.RunChatAgentAsync(new RunChatAgentRequest
                {
                    ChatThreadId = request.ChatThreadId,
                    UserId = request.UserId,
                    ChatAgent = chatAgent
                });

                assistantMessage = await chatMessageRepo.AddAsync(new ChatMessage
                {
                    ChatThreadId = request.ChatThreadId,
                    Role = ChatMessageRoles.Assistant,
                    Content = agentRun.AssistantContent,
                    CreatedBy = request.UserId,
                    UpdatedBy = request.UserId
                }, cancellationToken);

                if (assistantMessage is not null)
                {
                    await threadMemory.RefreshAsync(request.ChatThreadId, request.UserId, cancellationToken);
                }
            }
        }

        #endregion

        #region # Handle Result

        if (thread is null || thread.UserId != request.UserId)
        {
            result.Failure(ErrorCode.NotFound, "Chat thread not found.");
        }
        else if (userMessage is null || assistantMessage is null)
        {
            result.Failure(ErrorCode.InternalServerError, "Failed to create chat message.");
        }
        else
        {
            result.Success(new SendChatMessageResponse { UserMessage = userMessage, AssistantMessage = assistantMessage });
        }

        #endregion

        return result;
    }
}
