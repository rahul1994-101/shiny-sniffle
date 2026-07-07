# AI memory

Four context layers injected into each agent turn — in addition to the current user message.

**Integration point:** `ChatOrchestrator` composes layers before `AssistantAgent` / `EmailAgent` `RunAsync`.  
**Code home:** `src/WebApp/AI/Memory/`. No MediatR lib changes.

## Stack order (target)

```text
Agent system prompt
User memory              ← cross-thread
Thread memory (summary)  ← whole thread
Working memory           ← current task
Short-term messages      ← last N rows from ChatMessage
Current user message
```

## Layers

| # | Layer | Storage | Status |
|---|--------|---------|--------|
| 1 | **Short-term** | `ChatMessage` — last N rows | Done — `ChatMemoryLimits.ShortTermMessageLimit` (12) |
| 2 | **Thread** | `ChatThread.memorySummary`, `memorySummaryThroughMessageId` | Done — `ThreadMemoryService` rolls summary when count > 12 |
| 3 | **User** | `User` + `UserSetting`; future `UserMemoryFact` | Not injected into prompts yet |
| 4 | **Working** | Future `ChatThreadWorkingMemory` (JSON) or cache | Not started |

### Thread memory (phase A)

- **Before agent run:** if `memorySummary` exists, prepend system message with summary.
- **After successful send:** if message count > 12, incrementally summarize messages outside the window via gpt-4o-mini; persist on `ChatThread`.
- **Short-term window** remains the last 12 `ChatMessage` rows (source of truth for recent turns).

**DB migration** (existing databases):

```sql
ALTER TABLE [dbo].[ChatThread] ADD
    [memorySummary] NVARCHAR(MAX) NULL,
    [memorySummaryThroughMessageId] UNIQUEIDENTIFIER NULL;
```

Keep `src/Core/Database/dbo/Tables/ChatThread.sql` in sync.

### User memory (phase B — next)

- `UserMemoryService` loads profile + `UserSetting` (e.g. email config summary).
- Orchestrator injects as system context; agents unchanged.

### User facts (phase C)

- `UserMemoryFact` table for explicit “remember this” from chat.

### Working memory (phase D)

- Ephemeral task state for Email multi-step flows (list → pick → read → draft → send).

## Storage decisions

| Layer | v1 | Future |
|-------|-----|--------|
| Thread | Columns on `ChatThread` | `ChatThreadMemory` if versioning needed |
| User facts | — | `UserMemoryFact` (`userId`, content, source, `createdAt`) |
| Working | — | Separate JSON row/table; cleared on task complete |

Do **not** put user or working memory on `ChatThread`.

## Key files

```text
src/WebApp/AI/ChatOrchestrator.cs
src/WebApp/AI/Memory/ThreadMemoryService.cs
src/WebApp/AI/Memory/ChatMemoryLimits.cs
src/WebApp/Features/ChatMessages/Commands/SendChatMessage.cs
src/Core/Entities/ChatThread.cs
```

## Implementation order

| Phase | Layer | Notes |
|-------|--------|--------|
| A | Thread memory | Done |
| B | User memory | Inject profile/settings |
| C | User memory | Explicit “remember this” |
| D | Working memory | Email multi-step flows |
