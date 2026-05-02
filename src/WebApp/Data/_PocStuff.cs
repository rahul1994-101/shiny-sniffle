using WebApp.Models;

namespace WebApp.Data;

public sealed class Service
{
    private readonly Repository _repo;

    public Service(Repository repo)
    {
        _repo = repo;
    }

    public IReadOnlyList<ChatThread> GetThreadsOrdered() => _repo.GetThreadsOrdered();

    public ChatThread? TryGetThread(Guid threadId) => _repo.TryGetThread(threadId);

    public IReadOnlyList<ChatMessage> GetMessages(Guid threadId) => _repo.GetMessages(threadId);

    /// <summary>Oldest thread in the workspace; used when the URL has no or invalid <c>thread</c> query.</summary>
    public Guid GetDefaultThreadId() => _repo.GetDefaultThreadId();

    public Guid CreateThread() => _repo.CreateThread();

    public void DeleteThread(Guid threadId) => _repo.DeleteThread(threadId);

    public void RenameThread(Guid threadId, string title) => _repo.RenameThread(threadId, title);

    public void ClearThread(Guid threadId) => _repo.ClearThread(threadId);

    public Task ProcessUserMessageAsync(Guid threadId, string text, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var trimmed = text.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return Task.CompletedTask;
        }

        _repo.AddMessage(threadId, new ChatMessage { Role = "user", Content = trimmed });
        _repo.AddMessage(
            threadId,
            new ChatMessage { Role = "assistant", Content = ChatMocks.AssistantReply(trimmed) }
        );
        return Task.CompletedTask;
    }
}

public sealed class Repository
{
    private readonly List<ChatThread> _threads = [];

    public Repository()
    {
        CreateThreadInternal();
    }

    public IReadOnlyList<ChatThread> GetThreadsOrdered() =>
        _threads.OrderByDescending(t => t.UpdatedUtc).ToList();

    public ChatThread? TryGetThread(Guid threadId) => _threads.FirstOrDefault(t => t.Id == threadId);

    public IReadOnlyList<ChatMessage> GetMessages(Guid threadId)
    {
        var thread = TryGetThread(threadId);
        return thread is null ? Array.Empty<ChatMessage>() : thread.Messages;
    }

    public Guid GetDefaultThreadId() => _threads[0].Id;

    public Guid CreateThread()
    {
        var t = CreateThreadInternal();
        return t.Id;
    }

    public void DeleteThread(Guid threadId)
    {
        var idx = _threads.FindIndex(t => t.Id == threadId);
        if (idx < 0)
        {
            return;
        }

        _threads.RemoveAt(idx);

        if (_threads.Count == 0)
        {
            CreateThreadInternal();
        }
    }

    public void RenameThread(Guid threadId, string title)
    {
        var thread = TryGetThread(threadId);
        if (thread is null)
        {
            return;
        }

        var trimmed = title.Trim();
        thread.Title = string.IsNullOrEmpty(trimmed) ? "New chat" : trimmed[..Math.Min(trimmed.Length, 80)];
        thread.UpdatedUtc = DateTimeOffset.UtcNow;
    }

    public void ClearThread(Guid threadId)
    {
        var thread = TryGetThread(threadId);
        if (thread is null)
        {
            return;
        }

        thread.Messages.Clear();
        thread.Title = "New chat";
        thread.UpdatedUtc = DateTimeOffset.UtcNow;
    }

    public void AddMessage(Guid threadId, ChatMessage message)
    {
        var thread = TryGetThread(threadId) ?? throw new InvalidOperationException("Unknown thread.");
        thread.Messages.Add(message);
        thread.UpdatedUtc = DateTimeOffset.UtcNow;

        if (thread.Title == "New chat" && message.Role == "user")
        {
            thread.Title = TrimTitle(message.Content);
        }
    }

    private ChatThread CreateThreadInternal()
    {
        var t = new ChatThread();
        _threads.Add(t);
        return t;
    }

    private static string TrimTitle(string content)
    {
        var oneLine = content.ReplaceLineEndings(" ").Trim();
        if (oneLine.Length <= 40)
        {
            return string.IsNullOrEmpty(oneLine) ? "New chat" : oneLine;
        }

        return oneLine[..40].TrimEnd() + "…";
    }
}
