namespace WebApp.Components.Shared;

/// <summary>Ordered setup steps for new users (Settings → Workspace → Chat).</summary>
public sealed record OnboardingJourneyStep(string Href, string Title, string Lead, NavIconName? Icon = null);

public static class OnboardingJourney
{
    public static readonly OnboardingJourneyStep AfterGeneral = new(
        "/settings/email/providers",
        "Email providers",
        "Confirm server presets for your mail host.",
        NavIconName.EmailProviders);

    public static readonly OnboardingJourneyStep AfterEmailProviders = new(
        "/workspace/email/accounts",
        "Email accounts",
        "Connect your inbox and test the connection.",
        NavIconName.EmailAccounts);

    public static readonly OnboardingJourneyStep AfterEmailAccounts = new(
        "/chat",
        "Chat",
        "Run your first Email triage with the agent.");
}
