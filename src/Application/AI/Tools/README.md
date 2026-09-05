# Tools (`Application/AI/Tools/`)

MAF tool factories paired with agents.

| Naming | Role |
|--------|------|
| `{Domain}TriageTools` | Tools for a Triage agent. Example: `EmailTriageTools`. |
| Other `*Tools` | Tools for non-triage agents when added. |
| `MailboxReadHelpers.cs` | Email-tool builders, date parsing, and LLM text formatting — not the mail gateway. |
