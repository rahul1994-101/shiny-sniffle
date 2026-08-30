# Email read — implementation plan

> Index: [docs/README.md](README.md) · Business: [product.md](product.md) · Memory layers: [ai-memory.md](ai-memory.md)

Roadmap for mailbox capabilities: **EmailTriageAgent** + **EmailTriageTools** → `WorkspaceMailboxService` → `IMailboxService` (`MailKitMailboxService`).

---

## Status

```
Infrastructure/Mailbox (IMailboxService)   ✅ complete — stable port
Layers 0–5 + commands + @mailbox + 6a    ✅ shipped (Application/AI)
Application mailbox consumption           ✅ shipped (all port methods wired)
Layer 6b–6e                               deferred (compare, memory, actions)
```

---

## Infrastructure/Mailbox — complete ✅

Treat `src/Infrastructure/Mailbox/` as a **full mail-client adapter**. Do not add agent/tool concepts here.

| Region | Methods | Input / output |
|--------|---------|----------------|
| **Connection** | `TestConnectionAsync` | → `TestConnectionResult` |
| **Queries** | `ListMessagesAsync` | `ListMessagesFilters` → `ListMessagesResult` |
| | `GetMessagesAsync` | `MessageBatchFilters` → `GetMessagesResult` |
| | `GetAttachmentsAsync` | `GetAttachmentsFilters` → `GetAttachmentsResult` |
| | `ListFoldersAsync` | → `ListFoldersResult` |
| | `GetFolderAsync` | `GetFolderFilters` → `GetFolderResult` |
| **Commands** | `SendAsync` | `OutboundMail` → `SendMailResult` |
| | `SaveDraftAsync` | `OutboundMail` → `SaveDraftResult` |
| | `CopyMessagesAsync` | `MessageTransferFilters` → `CommandResult` |
| | `DeleteMessagesAsync` | `MessageBatchFilters` → `CommandResult` |
| | `MoveMessagesAsync` | `MessageTransferFilters` → `CommandResult` |
| | `SetMessageFlagsAsync` | `SetMessageFlagsFilters` → `CommandResult` |
| | `CreateFolderAsync` | `CreateFolderFilters` → `CommandResult` |

**Files:** `Abstractions.cs` · `DTOs.cs` (`EmailSettings`, `MailboxLimits`, filters/results) · `Helpers.cs` (Connection / Queries / Commands nested helpers) · `MailKitMailboxService.cs`.

**Conventions:** `conventions.mdc` — simple domain verbs on port; `...Filters` / `...Result` pairing; `MessageKey` for batch identity; Application maps `StoredMailboxSettings` → `EmailSettings` at the boundary.

---

## Application consumption — shipped ✅

**Goal:** wire the remaining port capabilities through Application facades and AI tools without duplicating infra logic.

### Stack

```text
SendChatMessage
  → EntityRefMentionContextService → WorkspaceReferenceService
  → ChatOrchestrator → EmailTriageAgent
        → EmailTriageTools.Session (cached MailboxAccountContext)
              → WorkspaceMailboxService    (account + filters → MailboxResult<T>)
                    → IMailboxService
```

| Layer | Key files |
|-------|-----------|
| **Chat entry** | `Features/Chat/ChatMessages/Commands/SendChatMessage.cs` |
| **Mentions** | `Features/Shared/References/Services.cs` (`EntityRefMentionContextService`) |
| **Agent** | `AI/Agents/EmailTriageAgent.cs` |
| **Tools** | `AI/Tools/EmailTriageTools.cs` |
| **References** | `Features/Shared/References/` — `Helpers.cs`, `Services.cs`, `DTOs.cs`, `Queries/SearchEntityRefMentions.cs` |
| **Gateway** | `Features/Shared/Services.cs` (`WorkspaceMailboxService`), `DTOs.cs` (`MailboxResult<T>`) |
| **Helpers** | `Features/Shared/MailboxReadHelpers.cs` |
| **Account DTOs** | `EmailAccounts/DTOs.cs` (`StoredMailboxSettings`, `MailboxAccountContext`) |

### Wired (tools + facades)

| Capability | Tool | Infra method |
|------------|------|--------------|
| Status | `get_mailbox_status` | `TestConnectionAsync` |
| List | `list_inbox_messages` | `ListMessagesAsync` |
| Open one | `get_inbox_message` | `GetMessagesAsync` (batch of 1) |
| Batch read | `get_inbox_messages` | `GetMessagesAsync` |
| Attachments | `get_attachments` | `GetAttachmentsAsync` |
| Folders | `list_mailbox_folders` | `ListFoldersAsync` |
| Folder stats | `get_folder` | `GetFolderAsync` |
| Send | `send_email` | `SendAsync` |
| Save draft | `save_draft` | `SaveDraftAsync` |
| Delete | `delete_messages` | `DeleteMessagesAsync` |
| Move | `move_messages` | `MoveMessagesAsync` |
| Copy | `copy_messages` | `CopyMessagesAsync` |
| Flags | `set_message_flags` | `SetMessageFlagsAsync` |
| Create folder | `create_folder` | `CreateFolderAsync` |

**Richer list filters:** `skip`, `body_contains`, `to_contains`, `attachments_filter` on `list_inbox_messages`.

**Richer send/draft:** `cc`, `bcc`, `html_body`, `mode` (new/reply/forward), `reply_uid` + `reply_folder`, `attachments` (`name|base64;…`) on `send_email` and `save_draft`.

### Application model notes

- **Persistence:** `StoredMailboxSettings` (Application) — not infra `EmailSettings`
- **Runtime:** `EmailSettingsMapping.ToMailRuntime()` before every `IMailboxService` call
- **Single-message get:** Application wrapper over `GetMessagesAsync` (not on infra port)
- **Batch filters:** agents call infra `MessageBatchFilters` / `MessageTransferFilters` directly via builders in `MailboxReadHelpers.cs`
- **Results:** `MailboxResult<T>` in `WorkspaceMailboxService`

---

## Shipped — read stack (Layers 0–5)

**Tools:** `get_mailbox_status` · `list_inbox_messages` · `get_inbox_message` · `get_inbox_messages` · `list_mailbox_folders`

| Layer | What users get |
|-------|----------------|
| **0 Foundation** | Mailbox configured/reachable; inbox list by `since` + `limit`; snippet previews (~120 chars); limits in `MailboxLimits` |
| **1 Time / volume** | Rich `since` (`today`, `yesterday`, `this_week`, `last_N_days`, date ranges); `count_only`; “N shown of M matched” when capped at 50 |
| **2 Filters** | `unread_only`, `from_sender`, `subject_contains` (AND with time range) |
| **3 Open one** | `get_inbox_message` by Uid or list `#N`; full plain-text body (12k cap); stable Uid per list row |
| **4 Rich content** | HTML → plain text; attachment names on get (download at infra — tool pending) |
| **5 Folders** | `folder` on list/get (inbox, sent, drafts, trash, junk, custom); `list_mailbox_folders` |

**Key Application types:** `MailboxListQuery`, `MailboxOpenRequest`, `MailboxListSnapshot`, `MailboxListRangeParser`, `EmailMailboxTextHelpers`.

---

## Shipped — commands + account resolution

| Capability | Tool / component |
|------------|------------------|
| **Send** | `send_email` |
| **Delete** | `delete_messages` (move to trash) |
| **Move / archive** | `move_messages` |
| **Flags** | `set_message_flags` (read, unread, flagged, unflagged) |
| **Batch read** | `get_inbox_messages` (max 5 Uids per call) |
| **Multi-account** | `mailbox_alias` param + `@mailbox:alias` mention auto-fill via `WorkspaceReferenceService.TryResolveMailboxAsync` |

**E2E mention flow:** user `@mailbox:alias` → `EntityRefMentionContextService.ResolveAsync` → `RunChatAgentRequest.DefaultMailboxAccount` → `EmailTriageTools.Session` default when tool omits `mailbox_alias`.

---

## Shipped — polish + infra refactor

Structural cleanup across Infrastructure and Application. Build verified.

| Area | What changed |
|------|----------------|
| **Infra port** | Full query/command surface; service owns orchestration; helpers are mechanical; DTO naming (`...Filters` / `...Result`, `MessageKey`, `MessageTransferFilters`, `CommandResult`) |
| **Settings split** | `StoredMailboxSettings` (Application persistence) vs `EmailSettings` (infra runtime) |
| **Application helpers** | `MailboxReadHelpers.cs` — parsers, builders, text formatting |
| **Account resolution** | `WorkspaceReferenceService` dispatches to `MailboxAccountResolver` — default account, alias, or `mailbox:alias` → `MailboxAccountContext` (upstream; not in gateway) |
| **Gateway** | `WorkspaceMailboxService` — resolved account + infra filters only; `MailboxResult<T>` |
| **AI tools** | `EmailTriageTools.Session` — per-turn state; `WithAccountHeader` on all outputs |

---

## Shipped — smart output contracts (6a)

Agent-only: output modes (`digest`, `triage`, `compare`, `single`, `stats`, `action_list`), tool choreography, `MaxDeepReadsPerTurn` (5), partial-coverage disclaimer, `SupportedUserPrompts` in `EmailTriageAgent`. No new tools.

---

## Layer 6 — Smart output (deferred)

**Goal:** Turn raw tool text into **useful answers**—digests, triage, comparisons, deep reads—without inventing content.

**Principle:** Tools fetch; agent interprets. Default flow: `list_inbox_messages` → selective `get_inbox_message` / `get_inbox_messages` (≤5 per turn).

### Remaining sub-layers

##### 6b — Compare helper

`compare_mail_periods` tool — two parsed ranges → two `count_only` calls.

##### 6c — Digest tool (optional)

`summarize_mail_scope` — defer if 6a choreography is fast enough.

##### 6d — Thread reference memory

`EmailMemory` snapshot per chat thread — see [ai-memory.md](ai-memory.md).

##### 6e — Action-oriented output

`action_list` sections for future workflows.

### Policies

| Constant | Value | Location |
|----------|-------|----------|
| `MaxDeepReadsPerTurn` | 5 | `EmailReadConstants` |
| `MaxDigestOptionalGets` | 3 | `EmailReadConstants` |
| `DefaultListLimit` | 20 | `MailboxLimits` |
| `MaxListLimit` | 50 | `MailboxLimits` |
| `MaxBatchGetCount` | 5 | `MailboxLimits` |
| `MaxBatchCommandCount` | 5 | `MailboxLimits` |

---

## Out of scope (this doc)

- **Scheduled send / brief** (product schedules — separate pass)
- Mailbox connection UI (Workspace → Email accounts)
- Further Infrastructure/Mailbox changes unless new IMAP/SMTP capability is required

---

## Suggested tickets

1. ~~**App consumption** — wire `GetAttachments`, `SaveDraft`, `Copy`, `GetFolder`, `CreateFolder` + richer list/send~~ ✅
2. **6b** — `compare_mail_periods` tool + formatter
3. **6d** — Thread last-list memory
4. **6e** — `action_list` hardening when action workflows start

Update this doc when a layer ships or priorities change.
