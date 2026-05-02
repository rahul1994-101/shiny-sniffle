using WebApp.Models;

namespace WebApp.Data;


public sealed class Service
{
    #region # Construction

    private readonly Repository _repo;

    public Service(Repository repo)
    {
        _repo = repo;
    }

    #endregion

    #region # Queries

    public Guid ActiveThreadId => _repo.ActiveThreadId;

    public string ActiveThreadTitle => _repo.ActiveThreadTitle;

    public IReadOnlyList<ChatMessage> Messages => _repo.Messages;

    public IReadOnlyList<ChatThread> ThreadsOrdered => _repo.ThreadsOrdered;

    #endregion

    #region # Messages

    public Task ProcessUserMessageAsync(string text, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var trimmed = text.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return Task.CompletedTask;
        }

        _repo.AddMessage(new ChatMessage { Role = "user", Content = trimmed });
        _repo.AddMessage(
            new ChatMessage { Role = "assistant", Content = ChatMocks.AssistantReply(trimmed) }
        );
        return Task.CompletedTask;
    }

    #endregion

    #region # Threads

    public void CreateNewThread() => _repo.CreateNewThread();

    public void SelectThread(Guid id) => _repo.SelectThread(id);

    public void DeleteThread(Guid id) => _repo.DeleteThread(id);

    public void RenameThread(Guid id, string title) => _repo.RenameThread(id, title);

    #endregion

    #region # Active conversation

    public void ClearActiveConversation() => _repo.ClearActiveConversation();

    #endregion
}

public sealed class Repository
{
    #region # Fields & construction

    private readonly List<ChatThread> _threads = [];
    private Guid _activeThreadId;

    public Repository()
    {
        var first = CreateThreadInternal();
        _activeThreadId = first.Id;
    }

    #endregion

    #region # Queries & active thread

    public Guid ActiveThreadId => _activeThreadId;

    public string ActiveThreadTitle => ActiveThread?.Title ?? "Chat";

    public IReadOnlyList<ChatMessage> Messages =>
        ActiveThread is null ? Array.Empty<ChatMessage>() : ActiveThread.Messages;

    public IReadOnlyList<ChatThread> ThreadsOrdered =>
        _threads.OrderByDescending(t => t.UpdatedUtc).ToList();

    private ChatThread? ActiveThread => _threads.FirstOrDefault(t => t.Id == _activeThreadId);

    #endregion

    #region # Messages

    public void AddMessage(ChatMessage message)
    {
        var thread = ActiveThread ?? throw new InvalidOperationException("No active thread.");
        thread.Messages.Add(message);
        thread.UpdatedUtc = DateTimeOffset.UtcNow;

        if (thread.Title == "New chat" && message.Role == "user")
        {
            thread.Title = TrimTitle(message.Content);
        }
    }

    #endregion

    #region # Threads

    public void CreateNewThread()
    {
        var t = CreateThreadInternal();
        _activeThreadId = t.Id;
    }

    private ChatThread CreateThreadInternal()
    {
        var t = new ChatThread();
        _threads.Add(t);
        return t;
    }

    public void SelectThread(Guid id)
    {
        if (_threads.Any(t => t.Id == id))
        {
            _activeThreadId = id;
        }
    }

    public void DeleteThread(Guid id)
    {
        var idx = _threads.FindIndex(t => t.Id == id);
        if (idx < 0)
        {
            return;
        }

        _threads.RemoveAt(idx);

        if (_threads.Count == 0)
        {
            var t = CreateThreadInternal();
            _activeThreadId = t.Id;
        }
        else if (_activeThreadId == id)
        {
            _activeThreadId = _threads[Math.Min(idx, _threads.Count - 1)].Id;
        }
    }

    public void RenameThread(Guid id, string title)
    {
        var thread = _threads.FirstOrDefault(t => t.Id == id);
        if (thread is null)
        {
            return;
        }

        var trimmed = title.Trim();
        thread.Title = string.IsNullOrEmpty(trimmed) ? "New chat" : trimmed[..Math.Min(trimmed.Length, 80)];
        thread.UpdatedUtc = DateTimeOffset.UtcNow;
    }

    #endregion

    #region # Active conversation

    public void ClearActiveConversation()
    {
        var thread = ActiveThread;
        if (thread is null)
        {
            return;
        }

        thread.Messages.Clear();
        thread.Title = "New chat";
        thread.UpdatedUtc = DateTimeOffset.UtcNow;
    }

    #endregion

    #region # Helpers

    private static string TrimTitle(string content)
    {
        var oneLine = content.ReplaceLineEndings(" ").Trim();
        if (oneLine.Length <= 40)
        {
            return string.IsNullOrEmpty(oneLine) ? "New chat" : oneLine;
        }

        return oneLine[..40].TrimEnd() + "…";
    }

    #endregion
}
