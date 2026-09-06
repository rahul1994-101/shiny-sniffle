# Tools (`Application/AI/Tools/`)

MAF tool factories paired with agents.

| Naming | Role |
|--------|------|
| `{Domain}TriageTools` | Tools for a Triage agent. Example: `EmailTriageTools`. |
| Other `*Tools` | Tools for non-triage agents when added. |
| `MailboxReadHelpers.cs` | Email-tool builders, date parsing, and LLM text formatting — not the mail gateway. |

`EmailTriageTools.Session` owns per-turn account cache, list snapshots (persisted per thread **and mailbox alias**), and `MaxDeepReadsPerTurn`. Follow-ups open by `list_index` from the saved list. Digest is list-only; triage gets ≤ `MaxTriageGets`. Send/delete/move/copy/create_folder/save_contact require `confirmed=true`. Recipients accept `contact:alias`. `list_email_accounts` + `summarize_all_inboxes` + `compare_mail_periods` + `search_contacts` + `save_contact` are in the catalog. `summarize_all_inboxes` resolves accounts sequentially then fetches folder stats in parallel. Mailbox tools accept alias, `mailbox:alias`, or the account email. Builders reject batch Uid counts above `MailboxLimits`. `last_week` is the previous Mon–Sun UTC.
