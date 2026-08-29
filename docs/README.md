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
| **Next** | **Manual refactor** of Email/AI flow — [email-read-implementation-plan.md](email-read-implementation-plan.md) § Code map |
| **Deferred** | Layer **6b** (`compare_mail_periods`), [ai-memory.md](ai-memory.md) user/working memory, Tags/Buckets admin UI |

### Recently shipped

- **Settings**; Workspace **Email accounts** + **Contacts**
- **Chat mentions** — `/` global search, `@` two-step picker, Tag/Bucket in picker, `EntityRefMentionText` bubbles (see **product.md §5.5**)
- **Email triage** — read (0–5) + commands (send/delete/move/flags) + batch get + `@mailbox:alias` resolution + Layer 6a output contracts
- **Mailbox stack** — `MailKitMailboxService` + helpers; `MailboxAccountResolver`; `MailboxReadHelpers`; `EmailTriageTools.Session` (per-turn state)
- **AI memory** — short-term window + thread summary roll-up

---

## Technical roadmaps

| Doc | Status |
|-----|--------|
| [email-read-implementation-plan.md](email-read-implementation-plan.md) | 0–5 + commands + 6a + polish ✅ · **refactor next** · 6b deferred |
| [ai-memory.md](ai-memory.md) | Thread ✅ · user/working planned |
| [design-system.md](design-system.md) | WebApp UI tokens, `ui-*`, glass, motion |

**Conventions:** [`.cursor/rules/`](../.cursor/rules/)

---

## How to use this folder

- **Product / GTM / pricing** → `product.md`
- **Next engine feature** → relevant technical roadmap above
- Do not duplicate business strategy in technical docs; link to `product.md`
