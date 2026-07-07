using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

using Application.Features.ChatMessages;
using Application.Features.ChatThreads;

using AiChatMessage = Microsoft.Extensions.AI.ChatMessage;
using AiChatRole = Microsoft.Extensions.AI.ChatRole;

namespace Application.AI.Memory;

public sealed class ThreadMemoryService(
    IOptions<FoundryOptions> foundryOptions,
    IFoundryAgentFactory agentFactory,
    ChatThreadRepository chatThreadRepo,
    ChatMessageRepository chatMessageRepo)
{
    public async Task<IReadOnlyList<AiChatMessage>> EnrichHistoryAsync(
        Guid chatThreadId,
        IReadOnlyList<AiChatMessage> shortTermHistory,
        CancellationToken cancellationToken = default)
    {
        var memory = await chatThreadRepo.GetMemoryStateAsync(chatThreadId, cancellationToken);
        if (memory?.Summary is not { Length: > 0 } summary)
        {
            return shortTermHistory;
        }

        var contextMessage = new AiChatMessage(
            AiChatRole.System,
            $"""
            Earlier in this conversation (summarized from messages no longer in the recent window):
            {summary}
            """);

        var enriched = new List<AiChatMessage>(shortTermHistory.Count + 1) { contextMessage };
        enriched.AddRange(shortTermHistory);
        return enriched;
    }

    public async Task RefreshAsync(Guid chatThreadId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (!foundryOptions.Value.IsConfigured)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var count = await chatMessageRepo.CountByChatThreadIdAsync(chatThreadId, cancellationToken);
        if (count <= ChatMemoryLimits.ShortTermMessageLimit)
        {
            await chatThreadRepo.UpdateMemorySummaryAsync(
                chatThreadId,
                userId,
                summary: null,
                summaryThroughMessageId: null,
                updatedBy: userId,
                cancellationToken);
            return;
        }

        var beyondWindow = await chatMessageRepo.GetBeyondRecentWindowAsync(
            chatThreadId,
            ChatMemoryLimits.ShortTermMessageLimit,
            cancellationToken);

        if (beyondWindow.Count == 0)
        {
            return;
        }

        var memory = await chatThreadRepo.GetMemoryStateAsync(chatThreadId, cancellationToken);
        var messagesToFold = SelectMessagesToSummarize(beyondWindow, memory?.SummaryThroughMessageId);
        if (messagesToFold.Count == 0)
        {
            return;
        }

        var summary = await SummarizeAsync(memory?.Summary, messagesToFold, cancellationToken);
        if (string.IsNullOrWhiteSpace(summary))
        {
            return;
        }

        var throughMessageId = beyondWindow[^1].Id;
        await chatThreadRepo.UpdateMemorySummaryAsync(
            chatThreadId,
            userId,
            summary.Trim(),
            throughMessageId,
            userId,
            cancellationToken);
    }

    private static List<ChatMessageDto> SelectMessagesToSummarize(
        IReadOnlyList<ChatMessageDto> beyondWindow,
        Guid? summaryThroughMessageId)
    {
        if (summaryThroughMessageId is null)
        {
            return beyondWindow.ToList();
        }

        var throughIndex = beyondWindow.ToList().FindIndex(m => m.Id == summaryThroughMessageId);
        if (throughIndex < 0)
        {
            return beyondWindow.ToList();
        }

        if (throughIndex >= beyondWindow.Count - 1)
        {
            return [];
        }

        return beyondWindow.Skip(throughIndex + 1).ToList();
    }

    private async Task<string?> SummarizeAsync(
        string? existingSummary,
        IReadOnlyList<ChatMessageDto> messages,
        CancellationToken cancellationToken)
    {
        var transcript = FormatTranscript(messages);
        var prompt = existingSummary is { Length: > 0 }
            ? $"""
               Update the conversation summary using the new messages below.
               Keep stable facts, decisions, and open questions. Be concise.

               Previous summary:
               {existingSummary}

               New messages:
               {transcript}
               """
            : $"""
               Summarize this conversation for future context. Keep stable facts, decisions, and open questions. Be concise.

               Messages:
               {transcript}
               """;

        var agent = agentFactory.CreateAgent(
            FoundryDeployments.Gpt4oMini,
            "Thread memory",
            "Summarizes chat threads for long-term context.",
            "You produce concise conversation summaries. Output only the summary text.");

        var response = await agent.RunAsync(
            [new AiChatMessage(AiChatRole.User, prompt)],
            cancellationToken: cancellationToken);

        return response.Messages.LastOrDefault(m => m.Role == AiChatRole.Assistant)?.Text;
    }

    private static string FormatTranscript(IReadOnlyList<ChatMessageDto> messages)
    {
        var builder = new System.Text.StringBuilder();
        foreach (var message in messages)
        {
            builder.Append(message.Role);
            builder.Append(": ");
            builder.AppendLine(message.Content);
        }

        return builder.ToString();
    }
}
