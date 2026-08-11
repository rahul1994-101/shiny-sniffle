using System.Text.RegularExpressions;

namespace WebApp.Components.Shared;

public static partial class ColorPickerPresets
{
    public const string Default = "#6366f1";

    /// <summary>Nine presets + custom picker — fits one row in settings editors.</summary>
    public static readonly string[] All =
    [
        "#6366f1",
        "#8b5cf6",
        "#ec4899",
        "#ef4444",
        "#f97316",
        "#eab308",
        "#22c55e",
        "#06b6d4",
        "#64748b"
    ];

    public static bool TryNormalize(string? input, out string? hex)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            hex = null;
            return true;
        }

        var candidate = input.Trim();
        if (!candidate.StartsWith('#'))
        {
            candidate = $"#{candidate}";
        }

        if (candidate.Length == 7 && HexRegex().IsMatch(candidate))
        {
            hex = candidate.ToLowerInvariant();
            return true;
        }

        hex = null;
        return false;
    }

    public static bool IsPreset(string? hex) =>
        hex is not null && All.Contains(hex, StringComparer.OrdinalIgnoreCase);

    [GeneratedRegex("^#[0-9a-fA-F]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex HexRegex();
}
