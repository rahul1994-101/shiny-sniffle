# Email read — implementation plan

> Index: [docs/README.md](README.md) · Memory layers: [ai-memory.md](ai-memory.md)

Roadmap for mailbox **read** capabilities: Email agent + `UserMailboxService` / `MailKitMailboxService` / `EmailTools`.

**Current baseline:** `list_inbox_messages` (folder + filters, Uid per row), `get_inbox_message` (HTML→text, attachments), `list_mailbox_folders`, `get_mailbox_status`.

---

## Read dimensions (user-facing)

| Dimension | Examples |
|-----------|----------|
| **Scope** | Inbox, unread, from sender, subject keyword, attachment, folders, one message, thread |
| **Time** | Recent, today, yesterday, last 7 days, since date, date range, new since last check |
| **Volume** | Last N, all in range (paginated), count only |
| **Depth** | List, snippet, full body, HTML, headers, attachments |
| **Output** | Raw list, summary, action items, priority, compare periods |
| **Mailbox state** | Configured, reachable, connection errors |
| **Reference** | Latest, Nth in list, match sender/subject, UID, chat context |

---

## Implementation layers (build order)

Work **one layer at a time**. Within a layer, pick **one dimension slice** (e.g. unread before from-subject).

### Layer 0 — Foundation ✅ (reviewed)

- Mailbox configured / reachable (`get_mailbox_status`, Settings → Email guard on agent switch)
- List inbox by `since` + `limit` (`list_inbox_messages`)
- Snippet list (preview up to 120 chars; not full bodies)
- Shared limits/copy in `EmailReadConstants`
- Tool descriptions + parameter hints on read tools
- Agent tool rules aligned with preview-only reads

### Layer 1 — List improvements ✅

**Dimensions:** Time + Volume

| Build | Status |
|-------|--------|
| Richer `since` | `this_week`, `last_N_days`, `yyyy-MM-dd..yyyy-MM-dd`, single-day ISO |
| `yesterday` / single-day bounds | Exclusive `UntilUtc` (not “since forever”) |
| Count-only | `count_only` on `list_inbox_messages` |
| Volume note | List shows “N shown of M matched” when capped at 50 |

*Parser: `InboxListRangeParser`; query: `InboxQuery.UntilUtcExclusive`, `CountOnly`; result: `InboxListResult`.*

### Layer 2 — IMAP filters ✅

**Dimension:** Scope (narrow inbox)

| Build | MailKit |
|-------|---------|
| Unread only | `NotSeen` |
| From sender | `FromContains` |
| Subject keyword | `SubjectContains` |
| Combined | Filters AND existing `since` / date range |

*`InboxQuery`: `UnreadOnly`, `FromContains`, `SubjectContains`. Tool: `unread_only`, `from_sender`, `subject_contains` on `list_inbox_messages`.*

### Layer 3 — Open one message ✅

**Dimensions:** Depth + Reference

| Build | Status |
|-------|--------|
| `get_inbox_message` (uid or list_index + matching list query) | Full plain-text body |
| Stable id in list | `Uid` + `#N` on each `InboxMessageSummary` row |

*`InboxMessageDetail`; `IMailboxService.GetInboxMessageAsync`; body capped at 12k chars.*

### Layer 4 — Rich content ✅

**Dimension:** Depth (extends Layer 3 fetch)

| Build | Status |
|-------|--------|
| HTML body → plain text | `EmailMessageBodyHelpers` HTML strip on get + snippets |
| Attachment names | Listed on `get_inbox_message` (no download) |

*Attachment download deferred (storage/API).*

### Layer 5 — Beyond inbox ✅

**Dimension:** Scope (folders)

| Build | Status |
|-------|--------|
| `folder` on list/get | inbox (default), sent, drafts, trash, junk, custom name |
| `list_mailbox_folders` | Name, path, role alias discovery |

*Threads deferred (provider-specific).*

### Layer 6 — Smart output (planned)

**Dimension:** Output — turn raw tool text into **useful answers**, not mail dumps.

Layers 0–5 fetch mail reliably. Layer 6 defines **how the Email agent thinks, formats, and choreographs tools** so users get digests, triage, comparisons, and deep reads—without inventing content. It also lays groundwork for **folder-specific actions** you’ll add later.

#### Principles

| Principle | Why |
|-----------|-----|
| **Tools fetch; agent interprets** | Never summarize or prioritize without tool output for the messages in scope. |
| **List before deep read** | Default: `list_inbox_messages` → selective `get_inbox_message` on 1–5 Uids. Avoid N full-body fetches. |
| **Explicit output shape** | User should see labeled sections (Summary, Needs reply, FYI, Counts)—not walls of headers. |
| **Bounded cost** | Cap deep reads (`EmailReadConstants` policy); say when results are partial (“summarized 5 of 23”). |
| **Same folder + query** | List/get/compare must reuse `folder`, `since`, and filters so Uids stay valid. |
| **Action-ready sections** | Use stable headings future workflows can parse or the user can say “do that for item 2”. |

#### User intents → output mode

| Intent | Example prompt | Output mode | Tool choreography |
|--------|----------------|-------------|-------------------|
| **Skim** | “What’s new today?” | `digest` | `list` (today, limit 20) → optional 0–3 `get` for ambiguous previews |
| **Triage** | “What needs my attention?” | `triage` | `list` (unread + today/this_week) → `get` top 3–5 by subject/sender → Needs reply / FYI / Low |
| **Deep one** | “Summarize the Amazon invoice” | `single` | `list` (filter) → `get` one Uid → bullets + attachments |
| **Count / delta** | “More mail than yesterday?” | `compare` | `count_only` × 2 (today vs yesterday) or this_week vs last_week |
| **Volume overview** | “Who emailed me most this week?” | `stats` | `list` (this_week, limit 50) → agent groups by sender from list rows only |
| **Sent review** | “What did I send Bob this week?” | `digest` | `list` (folder=sent, from filter if needed) → optional `get` for 1–2 |
| **Prep for action** | “Which invoices need paying?” | `action_list` | `list` (subject filter) → `get` each candidate (≤5) → `ACTION_ITEMS` section |

Add each row to `EmailAgent.SupportedUserPrompts` when shipped.

#### Sub-layers (build order)

Work **6a → 6e** in order. Each sub-layer is shippable on its own.

##### 6a — Output contracts (agent only) ✅

**Goal:** Consistent answer shapes without new tools.

| Deliverable | Status |
|-------------|--------|
| Output mode rules (`digest`, `triage`, `compare`, `single`, `stats`, `action_list`) | `EmailAgent` instructions |
| Tool choreography + `MaxDeepReadsPerTurn` ({5}) | `EmailReadConstants` + agent rules |
| Partial coverage disclaimer | Agent rule |
| `SupportedUserPrompts` per intent | `EmailAgent` |

*Constants: `DefaultDigestListLimit`, `MaxDigestOptionalGets`.*

##### 6b — Compare helper (thin backend)

**Goal:** Deterministic period comparison without the model doing arithmetic on fake counts.

| Build | Notes |
|-------|--------|
| `compare_mail_periods` tool **or** `output_mode=compare` on list | Two parsed ranges → two `count_only` (optional dual list) |
| Formatted result: “Today: 12 · Yesterday: 8 (+4)” | `EmailMailboxTextHelpers.FormatPeriodCompare` |
| Reuse `InboxListRangeParser` for `period_a` / `period_b` | Same filters/folder on both |

Prefer a **dedicated tool** if compare becomes common; keeps agent instructions smaller.

##### 6c — Digest tool (optional backend)

**Goal:** One call returns list-shaped text optimized for summarization (still previews unless `include_uids_for_get`).

| Build | Notes |
|-------|--------|
| `summarize_mail_scope` tool | Wraps list + returns structured preamble: query label, counts, rows #N/Uid/From/Subject/Preview |
| Params mirror `list_inbox_messages` + `deep_read_limit` (0–5) | When &gt; 0, service fetches bodies server-side and appends truncated bodies |
| Agent only synthesizes final narrative | Reduces multi-tool turns |

**Defer** if 6a choreography is enough; add when token/latency from multi-tool turns hurts.

##### 6d — Thread reference memory (Email memory)

**Goal:** “That one”, “the second Amazon email” works within a chat thread.

| Build | Notes |
|-------|--------|
| `AI/Memory/EmailMemory.cs` | After list/get tools run, persist last `{ folder, query fingerprint, [{ index, uid, from, subject }] }` per thread |
| Orchestration or tool hook saves snapshot | On `Features.SendChatMessageAsync` path after tool results (or tool-side if thread id available) |
| Agent rule: prefer stored Uid over re-list when user references “#2” / “that Amazon one” | Instructions |

**Out of scope for 6a:** cross-thread memory, “since last login”.

##### 6e — Action-oriented output (feeds your future actions)

**Goal:** Answers structured so **Sent/Drafts/custom-folder actions** can attach later.

| Build | Notes |
|-------|--------|
| `action_list` output contract | Sections: `ACTION_ITEMS`, `DRAFT_CANDIDATES`, `ARCHIVE_CANDIDATES` with Uid + folder + one-line reason |
| Agent never executes actions in Layer 6 | Only identifies candidates from tool text |
| Link to future `Workflows/` | e.g. “file to Archive”, “reply to thread” consume Uid + folder |

When you implement actions, they should accept **`(folder, uid)`** from Layer 6 output—no re-search by subject.

#### Constants & policies (add with 6a)

| Constant | Suggested value | Purpose |
|----------|-----------------|--------|
| `MaxDeepReadsPerTurn` | 5 | Max `get_inbox_message` calls per user turn |
| `DefaultDigestListLimit` | 20 | Skim/triage list size |
| `TriageUnreadSinceDefault` | today + this_week fallback | Agent rule when user says “attention” without dates |

#### What stays out of Layer 6

- Attachment download / open
- Thread/conversation grouping (provider-specific)
- Auto-send, auto-archive, rules engine (separate **actions** pass)
- LLM summarization inside C# services (keep summarization in the agent)

#### Code mapping (Layer 6)

| Sub-layer | `IMailboxService` | `EmailTools` / `Workflows` | Agent / Memory |
|-----------|-------------------|----------------------------|----------------|
| 6a | — | — | Instructions + `SupportedUserPrompts` |
| 6b | — (reuse list) | `compare_mail_periods` optional | Compare template |
| 6c | optional batch get | `summarize_mail_scope` optional | Shorter choreography rules |
| 6d | — | — | `EmailMemory.LoadAsync` / snapshot save |
| 6e | — | — | `action_list` template; future workflows |

#### Suggested tickets

1. **6a** — Output contracts + `MaxDeepReadsPerTurn` + prompt catalog rows
2. **6b** — `compare_mail_periods` tool + formatter
3. **6d** — Last-list snapshot per chat thread (if reference pain is high)
4. **6c** — Digest tool only if multi-tool latency is a problem
5. **6e** — `action_list` template when action workflows start

#### Test prompts (Layer 6 acceptance)

| Prompt | Pass if |
|--------|---------|
| “Summarize my inbox today” | Lists today first; ≤5 gets; labeled Summary; no invented mail |
| “What needs my attention?” | Unread + recent; Needs reply / FYI sections; cites Uids |
| “More email than yesterday?” | Two counts; numeric comparison; no fake totals |
| “Summarize the PayPal email from this week” | list + filter + one get; mentions attachments if present |
| “What did I send Bob this week?” | folder=sent; no inbox bleed |
| “Which messages look like invoices?” | subject/filter list; action candidates with Uid + folder |

*Parallel anytime after Layers 1–3; **recommended start: 6a only** (highest ROI, zero MailKit surface).*

---

## Sequence

```
0 Foundation     ✅
1 Time/Volume    ✅
2 Filters        ✅
3 Open one       ✅
4 Rich content   ✅
5 Folders        ✅
6 Smart output   ← 6a ✅; next 6b
```

**Rule:** Do not skip to Layer 5 before Layer 3. Users ask “read that email” more than “open Sent”.

---

## Code mapping (per layer)

| Layer | `IMailboxService` | `EmailTools` | Models |
|-------|-------------------|--------------|--------|
| 1 List+ | Extend `InboxQuery` | Extend `list_inbox_messages` | `InboxQuery` |
| 2 Filter | IMAP search in list ✅ | Filter args on list ✅ | `InboxQuery` filters |
| 3 Open | `GetInboxMessageAsync` ✅ | `get_inbox_message` ✅ | `InboxMessageDetail`, `Uid` on summary ✅ |
| 4 Rich | Body/attachments on get ✅ | Same get tool ✅ | `AttachmentNames`, `BodyFromHtml` |
| 5 Folders | `ListFoldersAsync`, folder on list/get ✅ | `list_mailbox_folders`, `folder` param ✅ | `MailboxFolderInfo` |
| 6a Contracts | — | — | Output modes + choreography ✅ |
| 6b Compare | — (reuse list/count) | `compare_mail_periods` (optional) | Compare template |
| 6c Digest | optional batch get | `summarize_mail_scope` (optional) | — |
| 6d Reference | — | — | `EmailMemory` last-list snapshot |
| 6e Actions prep | — | — | `action_list` template → future workflows |

---

## Out of scope (this doc)

- **Send** scenarios (separate pass)
- Settings UI (except “go to Settings → Email” messaging)
- Assistant agent routing

---

## Suggested first tickets

1. **Layer 6b** — `compare_mail_periods` tool (if compare prompts are common)
2. **Layer 6d** — Thread last-list memory (if “that email” reference is painful)
3. **Layer 6e** — `action_list` template hardening when folder/action workflows start
4. **Future** — attachment download API, thread support, 6c digest tool if needed

Update this doc when a layer ships or priorities change.
