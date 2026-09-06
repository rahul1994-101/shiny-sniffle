using System.Diagnostics;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Application.Models;

namespace Application.AI;

internal static class AgentResponseHelpers
{
    private static readonly TimeSpan StreamReportInterval = TimeSpan.FromMilliseconds(50);

    internal static string ExtractAssistantText(AgentResponse response)
    {
        var text = response.Messages.LastOrDefault(m => m.Role == ChatRole.Assistant)?.Text;
        return string.IsNullOrWhiteSpace(text)
            ? "I could not generate a response."
            : text.Trim();
    }

    internal static async Task<string> StreamAssistantTextAsync(
        AIAgent agent,
        IReadOnlyList<Microsoft.Extensions.AI.ChatMessage> messages,
        IProgress<ChatTurnProgress>? progress,
        string initialStatus,
        CancellationToken cancellationToken)
    {
        #region # Execute

        progress?.Report(new ChatTurnProgress(initialStatus, null));

        var updates = new List<AgentResponseUpdate>();
        var status = initialStatus;
        var lastReport = TimeSpan.Zero;
        var clock = Stopwatch.StartNew();

        await foreach (var update in agent.RunStreamingAsync(messages, cancellationToken: cancellationToken))
        {
            updates.Add(update);

            var nextStatus = ChatTurnStatusCopy.FromUpdate(update) ?? status;
            var statusChanged = !string.Equals(nextStatus, status, StringComparison.Ordinal);
            status = nextStatus;

            if (progress is null)
            {
                continue;
            }

            var elapsed = clock.Elapsed;
            if (!statusChanged && elapsed - lastReport < StreamReportInterval)
            {
                continue;
            }

            lastReport = elapsed;
            progress.Report(new ChatTurnProgress(status, ExtractPartialText(updates)));
        }

        var response = updates.ToAgentResponse();
        var finalText = ExtractAssistantText(response);
        progress?.Report(new ChatTurnProgress(status, finalText));

        #endregion

        #region # Handle Result

        return finalText;

        #endregion
    }

    private static string? ExtractPartialText(List<AgentResponseUpdate> updates)
    {
        if (updates.Count == 0)
        {
            return null;
        }

        var text = updates.ToAgentResponse().Messages.LastOrDefault(m => m.Role == ChatRole.Assistant)?.Text;
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }
}

internal static class ChatTurnStatusCopy
{
    internal static string? FromUpdate(AgentResponseUpdate update)
    {
        string? status = null;
        foreach (var content in update.Contents)
        {
            if (content is FunctionCallContent call)
            {
                status = ForTool(call.Name, call.Arguments);
            }
        }

        return status;
    }

    internal static string ForTool(string? name, IDictionary<string, object?>? arguments)
    {
        var alias = DisplayAlias(ReadArg(arguments, "mailboxAlias", "mailbox_alias"));
        var listIndex = ReadListIndex(arguments);
        var confirmed = ReadFlag(arguments, "confirmed");

        return name switch
        {
            "list_inbox_messages" => WithInbox("Checking", alias, "inbox…"),
            "get_inbox_message" => listIndex is > 0 ? $"Opening #{listIndex}…" : "Opening a message…",
            "get_inbox_messages" => "Opening messages…",
            "get_attachments" => listIndex is > 0 ? $"Checking attachments on #{listIndex}…" : "Checking attachments…",
            "summarize_all_inboxes" => "Checking all inboxes…",
            "get_mailbox_status" => WithInbox("Checking", alias, "connection…"),
            "get_folder" => "Looking at a folder…",
            "list_mailbox_folders" => WithInbox("Listing folders in", alias, "…"),
            "list_email_accounts" => "Listing connected accounts…",
            "compare_mail_periods" => "Comparing mail periods…",
            "search_contacts" => "Looking up contacts…",
            "save_contact" => confirmed == true ? "Saving a contact…" : "Preparing a contact…",
            "send_email" => confirmed == true ? "Sending…" : "Preparing a send…",
            "save_draft" => "Saving a draft…",
            "delete_messages" => confirmed == true ? "Deleting…" : "Preparing to delete…",
            "move_messages" => confirmed == true ? "Moving messages…" : "Preparing a move…",
            "copy_messages" => confirmed == true ? "Copying messages…" : "Preparing a copy…",
            "set_message_flags" => "Updating flags…",
            "create_folder" => confirmed == true ? "Creating a folder…" : "Preparing a folder…",
            _ => "Working…"
        };
    }

    private static string WithInbox(string verb, string? alias, string tail)
    {
        return string.IsNullOrEmpty(alias)
            ? $"{verb} {tail}"
            : $"{verb} {alias} {tail}";
    }

    private static string? DisplayAlias(string? alias)
    {
        if (string.IsNullOrWhiteSpace(alias) || alias.Contains('@'))
        {
            return null;
        }

        return alias.Trim();
    }

    private static int? ReadListIndex(IDictionary<string, object?>? arguments)
    {
        var raw = ReadArg(arguments, "listIndex", "list_index");
        return int.TryParse(raw, out var index) && index > 0 ? index : null;
    }

    private static bool? ReadFlag(IDictionary<string, object?>? arguments, string name)
    {
        var raw = ReadArg(arguments, name);
        if (raw is null)
        {
            return null;
        }

        if (bool.TryParse(raw, out var flag))
        {
            return flag;
        }

        return raw is "1" ? true : raw is "0" ? false : null;
    }

    private static string? ReadArg(IDictionary<string, object?>? arguments, params string[] keys)
    {
        if (arguments is null)
        {
            return null;
        }

        foreach (var key in keys)
        {
            foreach (var pair in arguments)
            {
                if (!string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var text = ValueText(pair.Value);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text.Trim();
                }
            }
        }

        return null;
    }

    private static string? ValueText(object? value)
    {
        return value switch
        {
            null => null,
            string text => text,
            bool flag => flag ? "true" : "false",
            JsonElement element => element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => element.ToString()
            },
            _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)
        };
    }
}
