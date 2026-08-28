# Documentation

## Business (single source)

| Doc | Purpose |
|-----|---------|
| **[product.md](product.md)** | Vision, personas, Solo / Pro / volume licensing, platform primitives (accounts, categories, contacts)—**no engineering** |

All commercial and product decisions should be updated in **`product.md`** only.

---

## Current focus (engineering)

**Done (recent):** Settings; Workspace Email accounts + Contacts; chat **rich editor** — `EntityRefMentionInput` with `/` global search (inserts `@kind:alias`), `@` two-step picker, server-side search, Tag/Bucket in picker, empty-workspace UX, styled clickable mentions in sent user bubbles (`EntityRefMentionText`). See **product.md §5.5** for product rules.

**Next:** **Email read + Email agent** — [email-read-implementation-plan.md](email-read-implementation-plan.md) Layer **6b** (`compare_mail_periods`) onward; parallel track [ai-memory.md](ai-memory.md) user/working memory when orchestration needs it.

**Deferred:** Tags/Buckets Workspace admin UI (picker + AI context for defs is shipped).

---

## Technical (engineering)

| Doc | Status |
|-----|--------|
| [email-read-implementation-plan.md](email-read-implementation-plan.md) | Mailbox read + Email agent layers 0–5, **6a done**; **6b+ next** |
| [ai-memory.md](ai-memory.md) | Thread memory done; user/working memory planned |
| [design-system.md](design-system.md) | WebApp UI tokens, `ui-*` components, glass, elevation, motion |

**Coding conventions:** [`.cursor/rules/`](../.cursor/rules/) (`solution.mdc`, `infrastructure.mdc`, `application.mdc`, `webapp.mdc`, `design-system.mdc`, `mediatr.mdc`).

---

## How to use this folder

- **Product / GTM / pricing / personas** → edit `product.md`.
- **How to build the next engine feature** → edit the relevant technical roadmap.
- Do not duplicate business strategy inside technical docs; link to `product.md` if context is needed.
