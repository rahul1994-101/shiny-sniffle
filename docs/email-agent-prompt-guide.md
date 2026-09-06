# Email agent — what to say

In the app this lives under **Help → Chat → Email agent** (`/help/chat/email-agent#what-to-ask`). An empty Email chat links there as **See every kind of prompt**.

Talk in Chat with the **Email** agent selected. You do not need special syntax. These are proven phrasings; mix them in your own words.

Connect mailboxes first under **Workspace → Email accounts**. Optional **notes** (Context) on each mailbox and each contact are passed to the agent when that inbox or person comes up — you do not need to repeat them in chat. New chats start on Email.

---

## How to name an inbox

| You want | Say |
|----------|-----|
| Default account | Nothing extra — just ask. |
| A specific account | `@mailbox:work`, `on my work inbox`, or the address (`you@work.com`). |
| Every account | `all my inboxes`, `both accounts`, `every mailbox`. |
| Follow-up in the same thread | After you listed one inbox, `#3` / `read that` stays on that inbox. |

Type `@` in the composer to pick a mailbox.

**Send, delete, move, copy, new folder, and save contact** always ask you to confirm before they happen. Marking read or flagged, and saving a draft, happen as soon as you ask.

---

## Accounts and connection

- How many email accounts are connected?
- Which mailboxes do I have?
- Check all my connected inboxes. Give unread and total counts per account.
- Is my work mailbox connected?
- Can you reach `you@work.com`?
- How should you treat my work inbox? (uses the notes you saved on that account)

---

## Daily loop (start here)

- What's new in my inbox today?
- Triage my unread mail from today. If nothing is unread today, widen to this week.
- What needs my attention?
- Which emails should I reply to first?
- Find unread messages that look like they need a reply. Start with the oldest.
- Give me a quick overview of this week's mail.

---

## Lists, counts, and filters

**When** — `today`, `yesterday`, `this week`, `last week`, `last 3 days`, or a range (`May 1 to May 7, 2026`).

**Where** — inbox (default), Sent, Drafts, Trash, Junk, or a folder name.

- Show my recent emails.
- What's in my inbox today?
- How many emails did I get today?
- Show my last 5 emails.
- Show my unread emails from today.
- Show emails from Amazon this week.
- Show emails from Sarah this week. (saved contact name or `@contact:sarah`)
- Find emails with invoice in the subject.
- Find emails mentioning quarterly review in the body.
- Show emails with attachments from today.
- Unread emails from PayPal this week.
- Show the next 20 emails after the first page from today.
- What did I send this week?
- Show my drafts.
- What folders are in my mailbox?
- How many unread messages are in my inbox?

---

## Read, summarize, compare

After a list, use `#1`, `#2`, or “the Amazon one.”

- Read the third email from today.
- Open message #2 from my unread list.
- Summarize the Amazon email from today.
- Open the top 3 unread messages from this week.
- Read that invoice email and tell me the attachment names.
- What's attached to that invoice? Name the files.
- Who emailed me the most this week?
- Compare how many messages I got today vs yesterday.
- Was this week busier than last week?
- Which invoices need paying?
- Scan this week's inbox for newsletters and bulk mail I can ignore.

---

## Act on mail

Confirm send, trash, move, copy, new folder, and save contact. Flags and drafts do not wait for a second yes.

**Flags**

- Mark #2 as read.
- Flag the invoice as important.

**Organize**

- Archive #3.
- Move those two spam messages to junk.
- Copy that email into Archive.
- Create a folder called Receipts.

**Delete**

- Trash the spam messages we just listed.

**Send and draft**

- Draft a reply to the most important unread from today. Do not send until I confirm.
- Reply to #2 saying thanks.
- Forward that invoice to alice@example.com with a short note.
- Send an email to alice@example.com about tomorrow's meeting.
- Email the team and CC bob@example.com.
- Save a draft to bob@example.com about the meeting.

---

## Contacts

Saved contacts are matched when mail is listed or opened. Their notes appear next to the message so the agent can treat that person as you described.

- What's Sarah's email?
- Look up contact Bob.
- Show emails from Sarah this week.
- Save Alice from that invoice as a contact.
- Remember bob@example.com as Bob. Note: client lead, always reply same day.
- What do we know about Sarah?

---

## Several inboxes in one chat

1. `Check all my connected inboxes.`
2. `List unread on @mailbox:work from today.`
3. `Triage unread on my personal inbox.`
4. Follow-ups (`read #1`, `draft a reply`) stay on the last inbox you listed.

---

## Assistant vs Email

| Need | Agent | Example |
|------|--------|---------|
| Inbox, send, folders, contacts | **Email** | What's new today? |
| How the app works, where to click | **Assistant** | Where do I connect my mailbox? |

Email will not do general knowledge or coding. Switch the picker in the composer to Assistant for that.

---

## Tips

- One ask per message when you want a clean list; then follow up (`read #2`, `draft a reply`) — the agent reuses that list and does not fetch every body on overviews.
- Name the **folder** if it is not inbox (`in Sent`, `in Drafts`).
- Put lasting facts in Workspace **Context** (mailbox: “personal, ignore newsletters”; contact: “VIP, reply same day”). Chat picks them up automatically.
- If a list says only part of the matches were shown, ask for the next page or a tighter filter.
- Destructive actions need an explicit yes in the same thread.
