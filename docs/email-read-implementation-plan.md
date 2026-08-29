# Email read — implementation plan

> Index: [docs/README.md](README.md) · Business: [product.md](product.md) · Memory layers: [ai-memory.md](ai-memory.md)

Roadmap for mailbox capabilities: **EmailTriageAgent** + **EmailTriageTools** → `UserMailboxService` / `MailboxAccountResolver` / `MailKitMailboxService`.

---

## Status

```
Layers 0–5 + commands + @mailbox + 6a   ✅ shipped
Polish pass (helpers, resolver, tools)  ✅ shipped
Manual Email/AI refactor                ← next (see Code map)
Layer 6b–6e                             deferred (`compare_mail_periods`, memory, actions)
```

---

## Shipped — read stack (Layers 0–5)

**Tools:** `get_mailbox_status` · `list_inbox_messages` · `get_inbox_message` · `get_inbox_messages` · `list_mailbox_folders`

| Layer | What users get |
|-------|----------------|
| **0 Foundation** | Mailbox configured/reachable; inbox list by `since` + `limit`; snippet previews (~120 chars); shared limits in `MailboxReadLimits` |
| **1 Time / volume** | Rich `since` (`today`, `yesterday`, `this_week`, `last_N_days`, date ranges); `count_only`; “N shown of M matched” when capped at 50 |
| **2 Filters** | `unread_only`, `from_sender`, `subject_contains` (AND with time range) |
| **3 Open one** | `get_inbox_message` by Uid or list `#N`; full plain-text body (12k cap); stable Uid per list row |
| **4 Rich content** | HTML → plain text; attachment names on get (no download yet) |
| **5 Folders** | `folder` on list/get (inbox, sent, drafts, trash, junk, custom); `list_mailbox_folders` |

**Key types:** `InboxQuery`, `InboxListRangeParser`, `InboxMessageDetail`, `MailboxFolderInfo`, `EmailMessageBodyHelpers`.

**Deferred from read stack:** attachment download, thread/conversation grouping (provider-specific).

---

## Shipped — commands + account resolution

| Capability | Tool / component |
|------------|------------------|
| **Send** | `send_email` |
| **Delete** | `delete_messages` (move to trash) |
| **Move / archive** | `move_messages` |
| **Flags** | `set_message_flags` (read, unread, flagged, unflagged) |
| **Batch read** | `get_inbox_messages` (max 5 Uids per call) |
| **Multi-account** | `mailbox_alias` param + `@mailbox:alias` mention auto-fill via `MailboxAccountResolver` |

**Stack:** `EmailTriageTools` → `UserMailboxService` → `MailboxAccountResolver` → `MailKitMailboxService`.

**E2E mention flow:** user `@mailbox:alias` → `EntityRefMentionContextService.TryResolveDefaultMailboxAliasAsync` → `RunChatAgentRequest.MailboxAlias` → `EmailTriageTools.Session` default when tool omits `mailbox_alias`.

---

## Shipped — polish pass

Structural cleanup before further feature work. Build verified.

| Area | What changed |
|------|----------------|
| **Application helpers** | Mailbox-specific types moved to `Features/Shared/MailboxReadHelpers.cs` (`InboxListRangeParser`, `EmailMailboxTextHelpers`, `EmailReadConstants`, …) |
| **Account resolution** | `MailboxAccountResolver` — default account, alias, or `mailbox:alias` → `MailboxAccountContext` |
| **User facade** | `UserMailboxService.RequireContextAsync` — throws on unresolved account (no silent empty/null) |
| **AI tools** | `EmailTriageTools.Session` — per-turn `userId` + default mailbox alias (no mutable scoped state) |
| **Tool output** | All tools prefix `Account: alias (email)` via `WithAccountHeader` |
| **Infrastructure** | `MailKitMailboxService` split into connection/search/summary/message/command helpers; `FetchAsync` for list snippets |
| **Agent catalog** | `EmailTriageAgent.SupportedUserPrompts` covers all current tools |

---

## Shipped — smart output contracts (6a)

Agent-only: output modes (`digest`, `triage`, `compare`, `single`, `stats`, `action_list`), tool choreography, `MaxDeepReadsPerTurn` (5), partial-coverage disclaimer, `SupportedUserPrompts` in `EmailTriageAgent`. No new tools.

---

## Layer 6 — Smart output (deferred)

**Goal:** Turn raw tool text into **useful answers**—digests, triage, comparisons, deep reads—without inventing content.

**Principle:** Tools fetch; agent interprets. Default flow: `list_inbox_messages` → selective `get_inbox_message` / `get_inbox_messages` (≤5 per turn).

### User intents → output mode

| Intent | Example | Mode | Choreography |
|--------|---------|------|--------------|
| Skim | “What’s new today?” | `digest` | list (today, ~20) → 0–3 optional gets |
| Triage | “What needs my attention?” | `triage` | list (unread + recent) → get top 3–5 → Needs reply / FYI / Low |
| Deep one | “Summarize the Amazon invoice” | `single` | list (filter) → get one Uid |
| Count / delta | “More mail than yesterday?” | `compare` | two `count_only` calls *(6b: dedicated tool)* |
| Volume | “Who emailed me most this week?” | `stats` | list (this_week) → group by sender from rows |
| Sent review | “What did I send Bob this week?” | `digest` | list (folder=sent) → optional gets |
| Prep for action | “Which invoices need paying?” | `action_list` | list → get ≤5 → `ACTION_ITEMS` |

### Remaining sub-layers

##### 6b — Compare helper

Deterministic period comparison—no model arithmetic on counts.

| Build | Notes |
|-------|--------|
| `compare_mail_periods` tool | Two parsed ranges → two `count_only` (optional dual list) |
| Formatted result | e.g. “Today: 12 · Yesterday: 8 (+4)” via `EmailMailboxTextHelpers.FormatPeriodCompare` |
| Params | `period_a` / `period_b` via `InboxListRangeParser`; same `folder` + filters on both |

##### 6c — Digest tool (optional)

`summarize_mail_scope` — one call returns structured list preamble (+ optional server-side deep reads). **Defer** if 6a multi-tool choreography is fast enough.

##### 6d — Thread reference memory

`EmailMemory` snapshot per chat thread: last `{ folder, query, [{ index, uid, from, subject }] }` so “#2” / “that Amazon one” resolves without re-list. See [ai-memory.md](ai-memory.md).

##### 6e — Action-oriented output

`action_list` sections (`ACTION_ITEMS`, `DRAFT_CANDIDATES`, `ARCHIVE_CANDIDATES`) with Uid + folder for future workflows. Agent identifies only—no execution in Layer 6.

### Policies

| Constant | Value | Location |
|----------|-------|----------|
| `MaxDeepReadsPerTurn` | 5 | `EmailReadConstants` (agent prompt) |
| `MaxDigestOptionalGets` | 3 | `EmailReadConstants` (agent prompt) |
| `DefaultListLimit` | 20 | `MailboxReadLimits` |
| `MaxListLimit` | 50 | `MailboxReadLimits` |
| `MaxBatchGetCount` | 5 | `MailboxReadLimits` |

### Out of scope (Layer 6)

Attachment download, thread grouping, auto-send/archive/rules, LLM summarization inside C# services, **scheduled send** (separate product track).

### Acceptance prompts

| Prompt | Pass if |
|--------|---------|
| “Summarize my inbox today” | Today first; ≤5 gets; labeled Summary; no invented mail |
| “What needs my attention?” | Unread + recent; Needs reply / FYI; cites Uids |
| “More email than yesterday?” | Two counts; numeric comparison; no fake totals |
| “Summarize the PayPal email from this week” | list + filter + one get; attachments if present |
| “What did I send Bob this week?” | folder=sent; no inbox bleed |
| “Which messages look like invoices?” | filter list; action candidates with Uid + folder |

---

## Code map

### Current stack (refactor starting point)

```text
WebApp  SendChatMessage
          → EntityRefMentionContextService (mention context + default mailbox alias)
          → ChatOrchestrator → EmailTriageAgent
                → EmailTriageTools.Session
                      → UserMailboxService
                            → MailboxAccountResolver
                            → IMailboxService (MailKitMailboxService)
                                  → MailboxConnectionHelpers
                                  → MailboxSearchHelpers / MailboxSummaryHelpers
                                  → MailboxMessageHelpers / MailboxCommandsHelpers
```

| Layer | Key files |
|-------|-----------|
| **Chat entry** | `Features/Chat/ChatMessages/Commands/SendChatMessage.cs` |
| **Mentions** | `Features/Shared/EntityRefMentionContextService.cs`, `EntityRefMentions.cs` |
| **Agent** | `AI/Agents/EmailTriageAgent.cs`, `AI/ChatOrchestrator.cs` |
| **Tools** | `AI/Tools/EmailTriageTools.cs` (`Session` nested class) |
| **App mailbox** | `Features/Shared/Services.cs` (`UserMailboxService`), `MailboxReadHelpers.cs` |
| **Account resolve** | `Features/Workspace/EmailAccounts/MailboxAccountResolver.cs`, `Repository.cs` |
| **Infrastructure** | `Infrastructure/Mailbox/` — `Abstractions.cs`, `DTOs.cs`, `Helpers.cs`, `MailKitMailboxService.cs`, `Mailbox*Helpers.cs` |

### Deferred feature work

| Sub-layer | `EmailTriageTools` | Agent / Memory |
|-----------|-------------------|----------------|
| 6b Compare | `compare_mail_periods` | Compare template |
| 6c Digest | `summarize_mail_scope` (optional) | Shorter choreography |
| 6d Reference | — | `EmailMemory` snapshot |
| 6e Actions | — | `action_list` → future workflows |

---

## Out of scope (this doc)

- **Scheduled send / brief** (product schedules — separate pass)
- Mailbox connection UI (Workspace → Email accounts; provider templates under Settings)
- Assistant agent routing

---

## Suggested tickets

1. **Refactor** — manual cleanup of Email/AI flow (boundaries, naming, error contracts) — **current**
2. **6b** — `compare_mail_periods` tool + formatter
3. **6d** — Thread last-list memory (if “that email” reference is painful)
4. **6e** — `action_list` hardening when action workflows start
5. **Future** — attachment download, thread support, 6c digest tool if latency hurts

Update this doc when a layer ships or priorities change.
