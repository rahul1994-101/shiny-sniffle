namespace WebApp.Components.Shared;

public enum AppModule
{
    Settings,
    Workspace,
    Chat,
    Workflows
}

public static class OnboardingStepIds
{
    public const string GeneralProfile = "settings.general";
    public const string EmailProviders = "settings.email-providers";
    public const string EmailAccounts = "workspace.email-accounts";
    public const string FirstTriage = "chat.first-triage";
}

public sealed record OnboardingStep(
    string Id,
    AppModule Module,
    string Href,
    string Title,
    string Lead,
    string What,
    string Why,
    NavIconName Icon,
    bool CountsTowardProgress = true);

public sealed record OnboardingModuleGroup(
    AppModule Module,
    string Label,
    NavIconName ModuleIcon,
    IReadOnlyList<OnboardingStep> Steps);

public static class OnboardingJourney
{
    public static readonly IReadOnlyList<OnboardingModuleGroup> Groups =
    [
        new(
            AppModule.Settings,
            "Settings",
            NavIconName.Settings,
            [
                new OnboardingStep(
                    OnboardingStepIds.GeneralProfile,
                    AppModule.Settings,
                    "/settings/general",
                    "General",
                    "Profile, mobile, and sign-in password.",
                    "Your name, contact details, and sign-in password.",
                    "So the app knows who you are and can secure your account before connecting mail.",
                    NavIconName.General),
                new OnboardingStep(
                    OnboardingStepIds.EmailProviders,
                    AppModule.Settings,
                    "/settings/email/providers",
                    "Email providers",
                    "Confirm IMAP/SMTP server presets for your mail host.",
                    "IMAP and SMTP server settings for your email host.",
                    "Presets tell the app how to reach your inbox when you connect an account.",
                    NavIconName.EmailProviders)
            ]),
        new(
            AppModule.Workspace,
            "Workspace",
            NavIconName.Workspace,
            [
                new OnboardingStep(
                    OnboardingStepIds.EmailAccounts,
                    AppModule.Workspace,
                    "/workspace/email/accounts",
                    "Email accounts",
                    "Connect your inbox, test the connection, and set a default.",
                    "A mailbox linked to your provider preset.",
                    "The Email agent needs a connected inbox to read messages and run triage.",
                    NavIconName.EmailAccounts)
            ]),
        new(
            AppModule.Chat,
            "Chat",
            NavIconName.Chat,
            [
                new OnboardingStep(
                    OnboardingStepIds.FirstTriage,
                    AppModule.Chat,
                    "/",
                    "First Email triage",
                    "Open a chat, choose the Email agent, and send your first triage message.",
                    "Your first chat with the Email agent.",
                    "Confirms everything works end-to-end — inbox connected, agent responding.",
                    NavIconName.Chat)
            ]),
        new(
            AppModule.Workflows,
            "Workflows",
            NavIconName.Workflows,
            [
                new OnboardingStep(
                    "workflows.coming-soon",
                    AppModule.Workflows,
                    "/workflows",
                    "Automations and rules",
                    "Scheduled triage and actions — coming later.",
                    "Scheduled triage and automated actions.",
                    "Optional later — set up mail first, then automate repeat work.",
                    NavIconName.Workflows,
                    CountsTowardProgress: false)
            ])
    ];

    public static readonly IReadOnlyList<OnboardingStep> ProgressSteps =
        Groups.SelectMany(g => g.Steps).Where(s => s.CountsTowardProgress).ToList();

    public static string GetModuleLabel(AppModule module) => module switch
    {
        AppModule.Settings => "Settings",
        AppModule.Workspace => "Workspace",
        AppModule.Chat => "Chat",
        AppModule.Workflows => "Workflows",
        _ => module.ToString()
    };

    public static OnboardingStep? TryGetNextStep(IReadOnlySet<string> completedIds)
    {
        foreach (var step in ProgressSteps)
        {
            if (!completedIds.Contains(step.Id))
            {
                return step;
            }
        }

        return null;
    }

    public static int GetStepNumber(OnboardingStep step)
    {
        for (var i = 0; i < ProgressSteps.Count; i++)
        {
            if (ProgressSteps[i].Id == step.Id)
            {
                return i + 1;
            }
        }

        return 0;
    }
}
