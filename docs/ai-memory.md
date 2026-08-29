# AI memory

Four context layers injected into each agent turn — in addition to the current user message.

**Integration:** `ChatOrchestrator` composes layers before `AssistantAgent` / `EmailTriageAgent` `RunAsync`.  
**Code:** `src/Application/AI/Memory/`

## Stack order (target)

```text
Agent system prompt
User memory              ← cross-thread (planned)
Thread memory (summary)  ← whole thread ✅
Working memory           ← current task (planned)
Short-term messages      ← last N rows ✅
Current user message
```

## Status

| Layer | Storage | Status |
|-------|---------|--------|
| **Short-term** | Last 12 `ChatMessage` rows | ✅ `ChatMemoryLimits.ShortTermMessageLimit` |
| **Thread** | `ChatThread.memorySummary`, `memorySummaryThroughMessageId` | ✅ `ThreadMemoryService` rolls summary when count > 12 |
| **User** | `User`; future `UserSetting` + `UserMemoryFact` | Not injected yet |
| **Working** | Future `ChatThreadWorkingMemory` (JSON) or cache | Not started |

**Shipped behavior (thread):** Before run, prepend `memorySummary` if present. After send, incrementally summarize messages outside the 12-row window (gpt-4o-mini) and persist on `ChatThread`. Columns in `Infrastructure/Database/chat/Tables/ChatThread.sql`.

---

## Planned

### User memory (phase B)

- `UserMemoryService` loads profile (later `UserSetting` for prefs / email config summary).
- Orchestrator injects as system context; agents unchanged.

### User facts (phase C)

- `UserMemoryFact` table for explicit “remember this” from chat.

### Working memory (phase D)

- Ephemeral task state for Email multi-step flows (list → pick → read → draft → send).
- Separate JSON row/table; cleared on task complete. **Do not** put on `ChatThread`.

### Email thread reference (ties to [email-read Layer 6d](email-read-implementation-plan.md))

- `EmailMemory` — last list/get snapshot per thread for “#2”, “that Amazon one”.

## Key files

```text
src/Application/AI/ChatOrchestrator.cs
src/Application/AI/Memory/ThreadMemoryService.cs
src/Application/AI/Memory/ChatMemoryLimits.cs
src/Application/Features/Chat/ChatMessages/Commands/SendChatMessage.cs
src/Infrastructure/Persistence/Chat/ChatThread.cs
```

## Build order

| Phase | Layer | Status |
|-------|--------|--------|
| A | Thread memory | ✅ Done |
| B | User memory | Next — inject profile/settings |
| C | User facts | “Remember this” |
| D | Working memory | Email multi-step flows |
