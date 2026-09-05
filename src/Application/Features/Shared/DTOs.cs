namespace Application.Features.Shared;
/// <summary>Consumer-neutral mailbox messages for the Application middle layer.</summary>
public static class MailboxMessages
{
    public const string WorkspaceEmailHint = "Connect your mailbox in Workspace → Email accounts.";

    public const string NotConfigured = $"Mailbox is not configured. {WorkspaceEmailHint}";

    public static string AccountNotFound(string aliasOrRef) =>
        $"No connected mailbox found for '{aliasOrRef}'. Check Workspace → Email accounts, or omit mailbox_alias to use the default account.";

    public static string IncompleteAccount(string emailAddress) =>
        $"Mailbox {emailAddress} is incomplete. Finish setup in Workspace → Email accounts.";
}
