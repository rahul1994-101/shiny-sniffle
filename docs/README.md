# Documentation

## Business (single source)

| Doc | Purpose |
|-----|---------|
| **[product.md](product.md)** | Vision, personas, licensing, platform primitives—**no engineering** |

All commercial and product decisions → **`product.md`** only.

---

## Current focus (engineering)

| | |
|---|---|
| **Next** | Scheduled in-app brief (optional habit); user memory facts |
| **Deferred** | User memory (profile/facts), persisted ActionItem table, Tags/Buckets assignment pickers |

### Recently shipped

- **Infrastructure/Mailbox** — full mail-client port (`IMailboxService`): connection, queries (list/get/attachments/folders), commands (send/draft/copy/delete/move/flags/create-folder); conventions locked in `conventions.mdc`
- **Settings**; Workspace **Email accounts** + **Contacts**
- **Chat mentions** — `/` global search, `@` two-step picker, Tag/Bucket in picker, `EntityRefMentionText` bubbles (see **product.md §5.5**)
- **Email triage** — read (0–5) + commands (send/delete/move/flags) + batch get + `@mailbox:alias` resolution + Layer 6a output contracts
- **Application mailbox** — `WorkspaceMailboxService`, `EmailTriageTools.Session`, `AI/Tools/MailboxReadHelpers`; `StoredMailboxSettings` ↔ `EmailSettings` split
- **AI memory** — short-term window + thread summary + Email last-list working memory
- **Email daily loop** — confirm-gated send/delete, contact recipients, compare tool; new chats default to Email with empty-state suggestions

---

## Technical roadmaps

| Doc | Status |
|-----|--------|
| [email-read-implementation-plan.md](email-read-implementation-plan.md) | Infra + app + 6a/6b/6d ✅ · scheduled brief later |
| [ai-memory.md](ai-memory.md) | Thread + Email list working memory ✅ · user memory planned |
| [design-system.md](design-system.md) | WebApp UI tokens, `ui-*`, glass, motion |

**Conventions:** [`.cursor/rules/`](../.cursor/rules/)

---

## How to use this folder

- **Product / GTM / pricing** → `product.md`
- **Next engine feature** → relevant technical roadmap above
- Do not duplicate business strategy in technical docs; link to `product.md`
