using Application.Features.Workspace.EmailAccounts;

namespace Application.Features.Shared;

/// <summary>Result of a workspace mailbox operation against a resolved account.</summary>
public sealed class MailboxResult<T> where T : class
{
    public MailboxAccountContext? Account { get; init; }

    public T? Value { get; init; }

    public string? Error { get; init; }

    public bool IsSuccess => Error is null && Value is not null;

    public static MailboxResult<T> Ok(MailboxAccountContext account, T value) =>
        new() { Account = account, Value = value };

    public static MailboxResult<T> Fail(string error) =>
        new() { Error = error };
}

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
