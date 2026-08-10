namespace Application.Features.workspace.EmailAccounts;

using Application.Features.Shared;

/// <summary>Public surface for mailbox alias preview (UI).</summary>
public static class EmailAccountAliases
{
    public static string StemFromEmailAddress(string emailAddress) =>
        EntityAliasRules.StemFromEmailAddress(emailAddress);
}
