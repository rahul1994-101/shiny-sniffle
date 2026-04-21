using WebApp.Models;

namespace WebApp.Data;

public sealed class Service
{
    #region # Construction

    private readonly AgenticApiClient _agenticApi;
    private readonly Repository _repo;

    public Service(Repository repo, AgenticApiClient agenticApi)
    {
        _repo = repo;
        _agenticApi = agenticApi;
    }

    #endregion

    #region # Queries

    public Guid ActiveThreadId => _repo.ActiveThreadId;

    public string ActiveThreadTitle => _repo.ActiveThreadTitle;

    public IReadOnlyList<ChatMessage> Messages => _repo.Messages;

    public IReadOnlyList<ChatThread> ThreadsOrdered => _repo.ThreadsOrdered;

    #endregion

    #region # Events

    public event Action? Changed
    {
        add => _repo.Changed += value;
        remove => _repo.Changed -= value;
    }

    #endregion

    #region # Messages

    public async Task ProcessUserMessageAsync(string text, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var trimmed = text.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return;
        }

        _repo.AddMessage(new ChatMessage { Role = "user", Content = trimmed });

        var baseUrl = _repo.EffectiveAgenticBaseUrl;
        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            var who = string.IsNullOrWhiteSpace(_repo.GmailUserEmail) ? null : _repo.GmailUserEmail.Trim();
            var (ok, reply, err) = await _agenticApi.MailAgentChatAsync(
                baseUrl,
                trimmed,
                who,
                cancellationToken
            );

            if (ok && !string.IsNullOrWhiteSpace(reply))
            {
                _repo.AddMessage(new ChatMessage { Role = "assistant", Content = reply });
                return;
            }

            var fallback = string.IsNullOrWhiteSpace(err)
                ? "Agent API returned an empty reply."
                : err;
            _repo.AddMessage(new ChatMessage { Role = "assistant", Content = fallback });
            return;
        }

        _repo.AddMessage(
            new ChatMessage { Role = "assistant", Content = ChatMocks.AssistantReply(trimmed) }
        );
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
