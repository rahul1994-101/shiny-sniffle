using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using WebApp.AI.Tools;
using WebApp.Utilities.Helpers;

namespace WebApp.AI.Agents;

public sealed class EmailAgent(IFoundryAgentFactory _agentFactory, EmailTools _emailTools)
{
    public async Task<RunChatAgentResponse> RunAsync(RunChatAgentRequest request, IReadOnlyList<Microsoft.Extensions.AI.ChatMessage> history, CancellationToken cancellationToken = default)
    {
        #region # Execute

        var tools = _emailTools.CreateTools(request.UserId, request.ChatThreadId);
        var agent = CreateEmailAgent(tools);
        var messages = history.ToList();
        var response = await agent.RunAsync(messages, cancellationToken: cancellationToken);

        #endregion

        #region # Handle Result

        return new RunChatAgentResponse
        {
            AssistantContent = ExtractAssistantText(response)
        };

        #endregion
    }

    #region # Supported User Prompts

    // Debug/test catalog — grouped by intent and capability. Not sent to the LLM.
    // Update when tools or read/send scope change (see docs/email-read-implementation-plan.md).

    private static readonly (string Intent, string Capability, string[] Prompts)[] SupportedUserPrompts =
    [
        ("Mailbox status", "get_mailbox_status", [
            "Is my email connected?",
            "Can you reach my mailbox?",
            "Check whether my mailbox is set up."
        ]),
        ("List inbox — recent", "list_inbox_messages (since today, previews only)", [
            "Show my recent emails.",
            "What's in my inbox today?",
            "List my latest messages."
        ]),
        ("List inbox — date range", "list_inbox_messages (since range or since+until)", [
            "Show messages from 2026-05-01 to 2026-05-07.",
            "List mail between May 1 and May 7, 2026.",
            "Emails from 2026-05-01..2026-05-07."
        ]),
        ("List inbox — time range", "list_inbox_messages (since: yesterday, this_week, last_N_days)", [
            "What came in yesterday?",
            "Show emails from this week.",
            "List mail from the last 3 days."
        ]),
        ("Count inbox", "list_inbox_messages (count_only true)", [
            "How many emails did I get today?",
            "Count my inbox messages from yesterday.",
            "How many messages this week?"
        ]),
        ("List inbox — volume", "list_inbox_messages (limit 1-50)", [
            "Show my last 5 emails.",
            "Give me the 10 most recent from today."
        ]),
        ("Filter inbox — unread", "list_inbox_messages (unread_only true)", [
            "Show my unread emails from today.",
            "How many unread messages do I have this week?",
            "List unread mail from yesterday."
        ]),
        ("Filter inbox — sender", "list_inbox_messages (from_sender)", [
            "Show emails from Amazon this week.",
            "Any mail from alice@example.com today?",
            "List messages from my boss in the last 3 days."
        ]),
        ("Filter inbox — subject", "list_inbox_messages (subject_contains)", [
            "Find emails with invoice in the subject.",
            "Show messages about 'project alpha' from this week."
        ]),
        ("Filter inbox — combined", "list_inbox_messages (since + unread_only + from_sender + subject_contains)", [
            "Unread emails from PayPal this week.",
            "How many unread invoices came in today?"
        ]),
        ("Smart output — digest", "output mode digest: list → 0-3 optional get", [
            "What's new in my inbox today?",
            "Give me a quick overview of this week's mail.",
            "Skim my recent emails and highlight what matters."
        ]),
        ("Smart output — triage", "output mode triage: unread list → get top 3-5", [
            "What needs my attention?",
            "Triage my unread mail from today.",
            "Which emails should I reply to first?"
        ]),
        ("Smart output — compare", "output mode compare: count_only × 2 periods", [
            "Do I have more email than yesterday?",
            "Compare how many messages I got today vs yesterday.",
            "Was this week busier than last week?"
        ]),
        ("Smart output — stats", "output mode stats: list only, group by sender", [
            "Who emailed me the most this week?",
            "Which senders dominated my inbox today?",
            "Top senders in my mail from the last 3 days."
        ]),
        ("Smart output — single", "output mode single: list (filter) → one get", [
            "Summarize the Amazon email from today.",
            "What does the latest invoice say? Read it and summarize.",
            "Give me the key points from the PayPal message this week."
        ]),
        ("Smart output — action_list", "output mode action_list: list → get ≤5 → ACTION_ITEMS", [
            "Which invoices need paying?",
            "What messages look like they need a reply today?",
            "Flag emails I should archive from this week."
        ]),
        ("Smart output — sent digest", "output mode digest + folder sent", [
            "Summarize what I sent this week.",
            "What did I send to Bob lately?",
            "Overview of my sent mail from yesterday."
        ]),
        ("Read one message — by Uid", "get_inbox_message (uid from list row)", [
            "Read the full email with Uid 42.",
            "Open that Amazon message — use the Uid from the list.",
            "Show me the complete body of Uid 100."
        ]),
        ("Read one message — by list index", "get_inbox_message (list_index + since/filters/folder matching list)", [
            "Read the third email from today.",
            "Open message #2 from my unread list this week.",
            "Show me the full text of the first Amazon email from yesterday."
        ]),
        ("List folders", "list_mailbox_folders", [
            "What folders are in my mailbox?",
            "Show my IMAP folders.",
            "Do I have a Sent or Archive folder?"
        ]),
        ("List sent mail", "list_inbox_messages (folder sent)", [
            "What did I send this week?",
            "Show my sent emails from yesterday.",
            "List the last 10 messages in Sent."
        ]),
        ("List drafts", "list_inbox_messages (folder drafts)", [
            "Show my drafts.",
            "Any draft emails from this week?"
        ]),
        ("Read sent message", "list_inbox_messages (folder sent) then get_inbox_message", [
            "Read the full email I sent to Bob yesterday.",
            "Open message #1 in Sent from this week."
        ]),
        ("Read with attachments", "get_inbox_message (attachment names in output)", [
            "Read that invoice email and tell me the attachment names.",
            "Open Uid 55 and list what files are attached."
        ]),
        ("Send email", "send_email (confirm to/subject/body first)", [
            "Send an email to alice@example.com.",
            "Email my team about tomorrow's meeting.",
            "Draft and send a message to bob@example.com — subject: Hello, body: ..."
        ]),
        ("Off-topic / wrong agent", "Decline; route to Assistant", [
            "What agents are in this app?",
            "What's 2 + 2?",
            "Write me a Python script."
        ])
    ];

    #endregion

    #region # Private Helpers

    private AIAgent CreateEmailAgent(IList<AITool> tools)
    {
        var modelDeployment = FoundryDeployments.Gpt54Mini;
        var name = "Email";
        var description = "Mailbox specialist that lists, summarizes, and sends mail through the connected account.";
        var maxGets = EmailReadConstants.MaxDeepReadsPerTurn;
        var digestLimit = EmailReadConstants.DefaultDigestListLimit;
        var optionalGets = EmailReadConstants.MaxDigestOptionalGets;
        var dateContext = EmailReadDateContext.AgentDateBlock();
        var todayIso = EmailReadDateContext.TodayUtcIso;
        var instructions = $"""
            You are the Email specialist for this workspace, not the general receptionist.

            {dateContext}

            Your job:
            - Help users read, summarize, and send email from their connected mailbox using your tools.
            - Use tools for every mailbox operation; turn tool results into clear, labeled answers—not raw dumps.
            - Do not guess or invent message contents, send outcomes, or mailbox status.

            Tool rules:
            - list_inbox_messages: previews (#N, Uid, from, subject, date). count_only for how-many. folder + since + filters as needed.
            - get_inbox_message: full body + attachment names. Always use folder + uid from a list row, or list_index with the same folder/since/filters as the list.
            - list_mailbox_folders: when folder names are unknown.
            - get_mailbox_status: when setup or connectivity is uncertain.
            - send_email: confirm to, subject, and body with the user first.
            - since (critical): prefer relative keywords—today, yesterday, this_week, last_week, last_N_days—for everyday requests. Empty means today.
              - When the user gives an explicit calendar range (e.g. "May 1 to May 7, {EmailReadDateContext.CurrentYear}"), pass either:
                - since=yyyy-MM-dd..yyyy-MM-dd (e.g. {EmailReadDateContext.CurrentYear}-05-01..{EmailReadDateContext.CurrentYear}-05-07), or
                - since=start yyyy-MM-dd and until=end yyyy-MM-dd (inclusive).
              - Do NOT pass only the start date for a range—the user asked for multiple days.
              - Never guess the year from training data. Today is {todayIso} UTC.
            - folder: inbox (default), sent, drafts, trash, junk, or name from list_mailbox_folders. Same folder on list and get.
            - limit: 1-50 (default {digestLimit}). Filters: unread_only, from_sender, subject_contains.

            Output choreography (Layer 6):
            - Tools fetch; you interpret. Never summarize or prioritize mail not returned by tools this turn.
            - Default flow: list_inbox_messages first, then selective get_inbox_message. Do not fetch full bodies for every row.
            - Max {maxGets} get_inbox_message calls per user turn. Prefer fewer when previews are enough.
            - When list output shows "N shown of M matched", tell the user coverage is partial (e.g. "summarized {digestLimit} of M").
            - Reuse the same folder, since, and filters across list and get in one turn so Uids stay valid.
            - Cite Uid (and folder when not inbox) when naming specific messages—for follow-ups and future actions.

            Output modes (pick one; use the matching section headings):
            - digest — skim/overview (e.g. "what's new today"). List (limit {digestLimit}); optional 0-{optionalGets} gets only if previews are too thin. Sections: Summary, Highlights (bullets with sender/subject), Counts.
            - triage — attention/reply priority. List unread + today; if empty widen to this_week. get up to {maxGets} messages that look actionable. Sections: Summary, Needs reply, FYI, Low priority (optional), Counts.
            - compare — more/less than another period. Two list_inbox_messages with count_only (same folder/filters; since today vs yesterday, or this_week vs last_week—use keywords, not ISO dates). Sections: Comparison (counts + delta in plain language), Brief note.
            - single — one message deep-read. List with filters → one get → bullets. Mention attachments if tool lists them. Sections: Summary, Key points, Attachments (if any).
            - stats — volume by sender. List only (up to 50); group by From from list rows—no get unless user asks. Sections: Summary, Top senders (ranked), Counts.
            - action_list — candidates for later action. List with filters → get up to {maxGets} candidates → Sections: Summary, ACTION_ITEMS (each: sender — subject — folder — Uid — one-line reason). Optional: FYI. Do not execute actions.

            Triage template (adapt with real tool data):
            Summary: (1-2 sentences)
            Needs reply
            - [Sender] — [Subject] (Uid: …) — why
            FYI
            - …
            Counts: …

            Boundaries:
            - Stay on topic: connected mailbox and email tasks only.
            - Do not answer general knowledge, coding help, trivia, chit-chat, or other off-topic requests.
            - For off-topic requests, decline briefly and redirect to an email task or the right agent switch.
            - If the request is not about email or the mailbox, tell the user:
              "This is handled by the Assistant agent. Please switch to Assistant using the agent selector in the chat composer."
            - Keep replies concise, warm, and professional.
            """;

        return _agentFactory.CreateAgent(modelDeployment, name, description, instructions, tools);
    }

    private static string ExtractAssistantText(Microsoft.Agents.AI.AgentResponse response)
    {
        var text = response.Messages.LastOrDefault(m => m.Role == Microsoft.Extensions.AI.ChatRole.Assistant)?.Text;
        return string.IsNullOrWhiteSpace(text)
            ? "I could not generate a response."
            : text.Trim();
    }

    #endregion
}
