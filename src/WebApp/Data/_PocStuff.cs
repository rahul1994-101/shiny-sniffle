using Microsoft.Extensions.Options;
using System.Text.Json;
using WebApp.Models;

namespace WebApp.Data;


public sealed class Service
{
    #region # Construction

    private readonly AgenticApiClient _agenticApi;
    private readonly IOptions<AgenticApiOptions> _agenticOptions;
    private readonly Repository _repo;

    public Service(Repository repo, AgenticApiClient agenticApi, IOptions<AgenticApiOptions> agenticOptions)
    {
        _repo = repo;
        _agenticApi = agenticApi;
        _agenticOptions = agenticOptions;
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

        var baseUrl = (_agenticOptions.Value.BaseUrl ?? "").Trim().TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            var (ok, reply, err) = await _agenticApi.MailAgentChatAsync(
                baseUrl,
                trimmed,
                userEmail: null,
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


public sealed class AgenticApiClient
{
    private static string FormatHttpError(System.Net.HttpStatusCode status, string raw)
    {
        var snippet = raw.Length <= 1200 ? raw : raw[..1200] + "...";
        return $"HTTP {(int)status}: {snippet}";
    }

    private static readonly JsonSerializerOptions JsonRead = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly JsonSerializerOptions JsonWrite = new()
    {
        // FastAPI / Pydantic expect snake_case JSON keys (message, user_email, …).
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        // Avoid culture-dependent number formatting if we add decimals later.
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.Strict,
    };

    private readonly IHttpClientFactory _httpClientFactory;

    public AgenticApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<(bool Ok, string Reply, string? Error)> MailAgentChatAsync(
        string baseUrl,
        string message,
        string? userEmail,
        CancellationToken cancellationToken = default
    )
    {
        var url = $"{baseUrl.TrimEnd('/')}/mail_agent_chat";
        var body = new MailAgentChatApiRequest { Message = message, UserEmail = userEmail };

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(5);
            using var response = await client.PostAsJsonAsync(url, body, JsonWrite, cancellationToken);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return (false, "", FormatHttpError(response.StatusCode, raw));
            }

            var env = JsonSerializer.Deserialize<ServiceEnvelopeDto>(raw, JsonRead);
            if (env is null)
            {
                return (false, "", "Invalid JSON from agent API.");
            }

            if (env.HasError)
            {
                return (false, "", ServiceEnvelopeDto.FormatErrors(env.Errors));
            }

            return (true, ServiceEnvelopeDto.FormatPayload(env.Payload), null);
        }
        catch (Exception ex)
        {
            return (false, "", ex.Message);
        }
    }
}

public static class Api
{
    public static WebApplication MapLangChainApi(this WebApplication app)
    {
        var g = app.MapGroup("/api/langchain").WithTags("LangChain");

        g.MapGet("/health", () => Results.Ok(new { status = "ok", at = DateTimeOffset.UtcNow }))
            .WithName("LangChainGatewayHealth");

        g.MapPost("/invoke", (SendChatRequestDTO body) =>
        {
            if (string.IsNullOrWhiteSpace(body.Message))
            {
                return Results.BadRequest(new { error = "message is required" });
            }

            var reply = ChatMocks.AssistantReply(body.Message);
            return Results.Ok(new SendChatResponseDTO { Reply = reply });
        })
            .WithName("LangChainInvoke");

        return app;
    }
}
