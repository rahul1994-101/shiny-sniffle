namespace Application.Features.Workspace.Contacts;

using Application.Features.Shared;

/// <summary>Public surface for contact alias preview (UI).</summary>
public static class ContactAliases
{
    public static string StemFromName(string firstName, string lastName) =>
        EntityAliasRules.StemFromPersonName(firstName, lastName);
}
