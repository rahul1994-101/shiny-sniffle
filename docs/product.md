# Product — business specification

> **This is the only business / product document.** Implementation, agents, and engineering roadmaps live elsewhere under `docs/` and `.cursor/rules/`. Do not mix commercial decisions with technical layer plans here.

**Status:** Direction for go-to-market and product shape. Revise when pricing or MVP scope is locked.

---

## 1. Vision

Build a **collection of Triage systems**—repeatable products that **ingest → categorize → prioritize → act → report** on a stream of work. **Email is the first** Triage system; others may follow later using the same platform ideas (accounts, categories, contacts, schedules, actions).

**Not the strategy:** one-off per-client codebases or bespoke “enterprise suites” built from the ground up. **Optional later:** light config/templates for large deals—not a second product line.

---

## 2. One-line promise (Email Triage)

Help people **stop living in their inbox**: a reliable morning (or scheduled) brief, a clear action queue, and approved actions—without replacing Gmail or Outlook as the mail client.

---

## 3. Who we serve (focus)

| Segment | Role in the business |
|---------|----------------------|
| **Individual** | Core user. Brief-first; minimal need to open the native inbox. |
| **Freelancer / pro solo** | Same product as individual, plus **automations** (rules, webhooks, higher limits). Almost the same UX and data model. |
| **Business (companies)** | **Volume licensing**—many seats of the **same** solo/pro experience. Central billing and legal; **not** a separate collaborative “agency” product in v1. |

**Explicitly out of scope for now:** agency-style shared inboxes, team assignment, org-wide rule admin, SSO-heavy enterprise suites. Those are a different category of solution; revisit only if volume deals demand specific checklist items (e.g. SSO), added as thin commercial layers—not a rewrite.

---

## 4. Commercial model

```text
Solo (individual)  →  Pro (freelancer: automations)  →  Volume (business: N seats, invoice, DPA)
```

- **Product-led growth:** one person gets value alone; word of mouth and volume purchases follow.
- **Business = seats × same app**, not “Business tier” with a different feature tree.
- **Pricing:** TBD; solo triage products often cluster around **~$10–30/mo** entry; Pro higher; volume = discount + contract.

---

## 5. Platform primitives (shared across Triage systems)

These apply to **Email Triage** first and define the long-term platform—not a one-off email feature list.

### 5.1 Connected accounts

- User may connect **multiple email accounts**.
- Each account has a **user-defined alias** (e.g. “Work”, “Client A”)—required; see **mailbox** ER in §5.5.
- **Schedules** control when mail is read / triage runs (per account or per saved preset).
- Briefs can be **per account** or **merged** into one queue (product decision when designing UX).

### 5.2 Categorization

- Users need a **durable organization model** for messages and related objects.
- **Workspace taxonomy (v1):** **tags** and **buckets** on **referable objects** (contacts and mailboxes first)—see **§5.5**. Mail and triage output use the same tag vocabulary later.
- **v1 direction:** flat **tags** on ERs; **buckets** for named groups (including user-named “organizations” with no separate Org entity).
- Triage **suggests** labels on mail/contacts; users **confirm**. Pro users add **rules** that apply tags when patterns match.
- **Category / subcategory** tree remains **optional later** if hybrid navigation is needed beyond buckets.

### 5.3 Contacts

- **Manual only**—no bulk auto-import from Google, LinkedIn, or address books at launch.
- User creates contacts for **easy recall** and stable rules (priority, tone, default tags).
- Each contact is a **referable object** (§5.5): `contact:{alias}` for AI and tools.
- **“Save as contact”** from a triage item is allowed; **silent harvest** of all correspondents is not.

### 5.4 Other shared concepts (names flexible)

| Concept | Purpose |
|---------|---------|
| **Saved triage preset** | Reusable scope: “Morning unread”, “Client mail this week”. |
| **Triage run** | One execution: inputs, structured result, history. |
| **Action item** | Something to do on a message (reply, snooze, webhook)—linked to account + message identity. |
| **Schedule** | When to run a preset; timezone; where to deliver (in-app first). |

**Actions** run against **labeled/categorized items** and **contacts** (e.g. reply, forward, call webhook)—Pro territory when automated.

### 5.5 Referable objects (ER), tags, and buckets

Shared **workspace taxonomy** for things the product (and AI) can point at consistently. Engineering uses handle prefixes `contact:`, `mailbox:`, `tag:`, and `bucket:`; this section is the business rules.

#### Referable objects (ER)

| ER | Role | AI handle |
|----|------|-----------|
| **Contact** | Person or role the user maintains for recall and rules | `contact:{alias}` |
| **Mailbox** | Connected email account (Workspace → Email accounts) | `mailbox:{alias}` |
| **Tag** | Label definition (facets, roles, topics) | `tag:{alias}` |
| **Bucket** | Named group definition (e.g. XYZ Inc, Family) | `bucket:{alias}` |

- Every ER **must** have a user-scoped **alias** (stable for tools and prompts) and may have **context** (optional facts for agents).
- **Color** on tags and buckets is UI-only.
- Additional ER kinds (e.g. message, thread) may be added later—not a single generic “entity” table.
- All ER rows and memberships are **scoped to one user** (solo/pro v1; not shared across seats).

#### Tags — *describe*

- **Job:** facets, flags, workflow hints (e.g. `vip`, `invoice`, `slow-payer`).
- **Fields:** **name** + **alias** + optional **context**; **color** (UI only).
- **Cardinality:** many tags per contact/mailbox via TagAssignment; same tag on many ERs.
- **Not in v1:** tagging a bucket (only contacts/mailboxes are tagged).

#### Buckets — *place*

- **Job:** simple named groups with clear membership (e.g. “Organizations”, “XYZ Inc”, “Client work”).
- **Fields:** **name** + **alias** + optional **context**; **color** (UI only). No bucket types or Org table in v1.
- **Cardinality:** many-to-many via BucketAssignment—contact/mailbox ERs can sit in **many** buckets; a bucket holds **many** ERs.
- AI and triage can **scope** to “everything in bucket X” by expanding membership to handles.

#### Example — organizing contacts

User-defined dictionaries (names are illustrative):

- **Contacts (ER):** Sarah, Molly, Marta, Sam, Tom, John — each with a stable alias for AI (e.g. `contact:sarah`).
- **Buckets:** `XYZ Inc`, `Family`, `Friends`
- **Tags:** `Sales`, `Marketing`, `Wife`, `Dad`, `Gaming`, `Dad Jokes`, …

**Membership and tags** (read as *Name* *[bucket(s)]* *[tag(s)]*):

| Contact | Buckets | Tags |
|---------|---------|------|
| Sarah | XYZ Inc | Sales |
| Molly | XYZ Inc | Marketing |
| Marta | Family | Wife |
| Sam | Family | Dad |
| Tom | Friends | Gaming |
| John | XYZ Inc, Friends | Marketing, Dad Jokes |

John shows **many buckets and many tags** on one person—work and social overlap without a separate “Organization” entity (`XYZ Inc` is just a bucket name).

**What the user gets:** filter by bucket (“everyone at XYZ Inc”), by tag (“all `Marketing`”), or combined; later, triage/AI can scope to the same groups (e.g. mail tied to contacts in `XYZ Inc`).

#### Lifecycle

- **Soft-delete an ER** (contact, mailbox, tag, or bucket): remove junction rows where applicable; definitions remain unless the row itself is deleted.
- **Rename** display name: UI uses the new name; **alias** (and handles) are unchanged unless the user edits alias.

#### Alias vs slug (two layers)

| Layer | Table / schema | Stable key | AI handles | Purpose |
|-------|----------------|------------|------------|---------|
| **Workspace ER** | `workspace.*` (Contact, EmailAccount, Tag, Bucket) | **`alias`** | yes — `{kind}:{alias}` | User-owned things agents and rules point at |
| **Catalog** | `dbo.EmailProvider` (and similar) | **`slug`** | no | Infra/templates (IMAP/SMTP presets); not workspace referables |

- **`name`** (ER) = display label — rename freely in UI.
- **`alias`** (ER) = stable handle — like `@username`; auto-generated from name when blank on save; unique per user per kind.
- **`context`** (ER) = optional facts for UI and agent prompts.
- **Catalog** rows use **`name`**, **`slug`**, and **`sortOrder`**.
- Do **not** add a separate **`slug`** column on Tag/Bucket; **`alias` already is the machine key.**

Engineering: `EntityRefs.Format` / `EntityRefs.TryParse` at boundaries; DB column is always `alias` on workspace ERs.

#### Mention syntax — future UI (not v1)

Social-style typing **on top of** existing handles — no extra DB columns.

| User types (draft) | Resolves to | Notes |
|--------------------|-------------|--------|
| `@sarah` | `contact:sarah` | Autocomplete contacts (and optionally other ER kinds) |
| `@work` | `mailbox:work` | Mailbox / connected inbox |
| `#marketing` or `@marketing` | `tag:marketing` | **`#`** optional hashtag affordance for tags |
| `@xyz-inc` | `bucket:xyz-inc` | Named group |

**Autocomplete:** as the user types `@…` or `#…`, show matches on **alias** and **name** across workspace ERs (kind badge + display name). On pick, store/canonicalize to **`{kind}:{alias}`** in prompts, rules, and tool args — not the free-form display name.

**Rules for implementers:**

1. **AI, tools, and rules** use **`kind:alias`** only (rename-safe).
2. **UI** shows **name**; chips/tooltips may show handle (e.g. `tag:marketing`).
3. **Tag/bucket assignment** on contacts/mailboxes stays **picker-based** (dictionary rows), not free-text at assign time.
4. **Triage suggestions** on mail: suggest → user confirms; do not auto-create tag/bucket defs from model output without explicit user action.
5. **Catalog `slug`** (e.g. `gmail`) stays in provider/mailbox config — never mixed into `@`/`#` ER mention resolution.

**Out of scope until mention UI lands:** chat composer `@`/`#` picker, rule builder token insertion, resolving mentions in stored prompt templates.

#### Relation to mail triage

- v1 implements tags/buckets on **contacts and mailboxes** first.
- Triage **suggestions** on mail reuse the same tag **aliases** (handles) once message-level assignment exists; until then, triage output can reference ERs and buckets already in workspace.

---

## 6. Personas and success

### 6.1 Individual

- **Job:** “I don’t want to **start** my day in Gmail.”
- **Day one:** Connect mail → first triage → **Summary**, **Needs reply**, **Counts**; honest “partial coverage” when volume is high.
- **Week one:** Scheduled brief **or** manual habit **5 weekdays** without opening the native inbox first.

### 6.2 Freelancer / consultant

- **Job:** Individual promise + **client mail** separated from noise + **some automation**.
- **Day one:** Rules or tags tied to **contacts** / senders; triage highlights client action.
- **Week one:** At least one **approved action** (e.g. send reply) or webhook; repeatable presets.

### 6.3 Business buyer (volume)

- **Job:** Equip **each employee** with the same triage cockpit; one contract.
- **Success:** Procurement happy with **seat count, invoice, DPA**; users same as Pro—not shared team inbox on day one.
- **Known gap to acknowledge in sales:** shared mailboxes (`support@`) are **one connector, one owner** until a future collaboration product exists.

### 6.4 Anti-personas (deprioritize)

- Buyers who only want bundled **Copilot/Gemini**—we sell **workflow and habit**, not generic chat.
- Users who want a **new mail client** (Superhuman, Spark).
- Buyers requiring **fully autonomous send** with no approval default.

---

## 7. Triage result standard (product quality bar)

Every channel (chat, scheduled brief, future digest email) should deliver the **same intent**:

| Section | Purpose |
|---------|---------|
| **Summary** | 1–2 sentences for the scoped period/query. |
| **Needs reply** | Actionable items with sender, subject, **which account**, stable message reference. |
| **FYI** | Optional lower priority. |
| **Counts** | Matched vs reviewed; comparison when user asks “more than yesterday?”. |
| **Coverage note** | When capped: “Reviewed 20 of 47 matching.” |

**Trust rules:**

- No invented mail or send outcomes—only what was actually read.
- **Send and destructive actions** require explicit user approval by default.
- Do **not train** on customer mail (messaging and vendor policy).

---

## 8. Individual vs Pro (same base, different depth)

| | Solo / Individual | Pro (freelancer) |
|---|-------------------|------------------|
| Accounts | Multiple + alias | Same, higher caps |
| Schedules | Core brief | More presets / frequency |
| Categories | Manual + triage suggestions | **Rules** auto-apply tags |
| Contacts | Manual | Same + linked automations |
| Actions | Approve send | + webhooks, higher volume |
| History | Shorter retention | Longer (TBD) |

Volume deals = **Pro (or agreed tier) × headcount**, not a different app.

---

## 9. Problem we solve

| Pain | Without triage |
|------|----------------|
| Volume | Everything feels urgent; backlog wins. |
| Interruption | Reactive checking kills deep work. |
| Missed signal | Money, clients, invoices lost in noise. |
| Many inboxes | One person, many accounts, one overwhelmed brain. |

**Triage** means: narrow the stream → prioritize → act on a **queue**, not read everything.

---

## 10. Positioning and competition (summary)

| Alternatives | Our angle |
|--------------|-----------|
| SaneBox, Fyxer | Sorting only—we add **narrative brief + action queue + schedule**. |
| AI “chief of staff” apps | Same job—we productize **structured triage + platform primitives** (contacts, tags). |
| Gmail / Outlook + Copilot | Bundled AI—we win on **habit, multi-account, honest queue**. |
| Custom Claude/Gmail setups | **Self-serve product**, not services. |

Invest in: **morning habit**, **multi-account**, **manual contacts + tags**, **approve-then-act**, **volume licensing** without forking the product.

---

## 11. MVP (business definition)

**Goal:** Prove **individual / freelancer** value and willingness to pay—not business platform features.

**In scope (conceptual)**

- Email as first Triage system.
- Connect at least one mailbox; run triage on demand; structured result standard above.
- Path to **scheduled brief** (habit) soon after first value moment.
- Foundation for **tags** and **manual contacts** (even if minimal UI at first).

**Out of scope for MVP**

- Team collaboration, shared inboxes, assignment.
- SSO, admin portal (volume can be manual invoicing initially).
- Auto-import contacts; attachment pipelines; full CRM.
- Second Triage system (non-email).

**Success metrics (directional)**

| Metric | Target |
|--------|--------|
| Mailbox connected within 7 days | ≥70% of signups |
| First successful triage within 24h of connect | ≥60% |
| Active triage ≥3 days in first 14 days | ≥40% of activated |
| North star (early) | Weekly users with ≥1 successful triage |

---

## 12. Open decisions (business)

1. **Brand name** — Email Triage vs broader “Triage platform” public story (Email first either way).
2. **Free tier** — Trial length vs forever-free with caps.
3. **First delivery channel after in-app** — digest email vs Slack vs stay in-app only.
4. **Tag-only v1 vs hybrid** — ER tags + name-only buckets on contacts/mailboxes (§5.5); hybrid category tree deferred.
5. **Contact promotion** — only explicit “add contact” vs “save from this message” (recommend latter, still manual save).

---

## 13. What this doc does not contain

- Repository layout, agents, MediatR, or layer numbers.
- Per-client implementation folders.
- Engineering phases and schema.

For technical work, use **`docs/README.md`** and the engineering roadmaps listed there.

---

*Last updated: §5.5 alias/slug layers + future @/# mention syntax.*
