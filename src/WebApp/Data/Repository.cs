using Microsoft.Extensions.Options;
using WebApp.Models;

namespace WebApp.Data;

public sealed class Repository
{
    #region # Fields & construction

    private readonly IOptions<AgenticApiOptions> _agenticOptions;
    private readonly List<ChatThread> _threads = [];
    private Guid _activeThreadId;

    public Repository(IOptions<AgenticApiOptions> agenticOptions)
    {
        _agenticOptions = agenticOptions;
        var first = CreateThreadInternal();
        _activeThreadId = first.Id;
    }

    #endregion

    #region # Agentic API / Gmail (session)

    /// <summary>Optional override; when empty, <see cref="AgenticApiOptions.BaseUrl"/> is used.</summary>
    public string AgenticApiBaseUrlOverride { get; set; } = "";

    /// <summary>Mailbox address from mock sign-in; sent to Python as user_email for Gmail token lookup.</summary>
    public string GmailUserEmail { get; private set; } = "";

    /// <summary>True after the user completes the mock login (email + any password).</summary>
    public bool HasMockSession { get; private set; }

    public string EffectiveAgenticBaseUrl
    {
        get
        {
            var o = AgenticApiBaseUrlOverride.Trim();
            if (!string.IsNullOrEmpty(o))
            {
                return o.TrimEnd('/');
            }

            return (_agenticOptions.Value.BaseUrl ?? "").Trim().TrimEnd('/');
        }
    }

    /// <summary>
    /// Mock sign-in: stores email for the server session (Blazor scoped circuit). Password is ignored.
    /// </summary>
    public void StartMockSession(string email, string? passwordIgnored = null)
    {
        _ = passwordIgnored;
        var e = (email ?? "").Trim();
        if (string.IsNullOrEmpty(e) || e.IndexOf('@', StringComparison.Ordinal) < 0)
        {
            throw new ArgumentException("A valid email address is required.", nameof(email));
        }

        GmailUserEmail = e;
        HasMockSession = true;
        Changed?.Invoke();
    }

    public void ClearMockSession()
    {
        GmailUserEmail = "";
        HasMockSession = false;
        _threads.Clear();
        var t = CreateThreadInternal();
        _activeThreadId = t.Id;
        Changed?.Invoke();
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

    #region # Events

    public event Action? Changed;

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

        Changed?.Invoke();
    }

    #endregion

    #region # Threads

    public void CreateNewThread()
    {
        var t = CreateThreadInternal();
        _activeThreadId = t.Id;
        Changed?.Invoke();
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
            Changed?.Invoke();
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

        Changed?.Invoke();
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
        Changed?.Invoke();
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
        Changed?.Invoke();
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
