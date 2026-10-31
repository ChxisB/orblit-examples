using System.Globalization;
using Orblit.Examples;

namespace Orblit.Gallery;

/// <summary>
/// Sets an example's settings from text, such as the <c>Map size=2048</c> that <c>ORBLIT_SET</c>
/// names, so that a sweep of one setting is a shell loop and the gallery holds no code for any
/// one example.
/// </summary>
internal static class SettingByLabel
{
    /// <summary>
    /// Sets the setting that <paramref name="item"/>, written <c>Label=value</c>, names, as a person
    /// using its control would. Says through <paramref name="complain"/> why it could not. The
    /// settings are asked for again each time, because a change can add or take away the next.
    /// </summary>
    public static void Set(Example example, string item, Action<string> complain)
    {
        var at = item.IndexOf('=', StringComparison.Ordinal);
        if (at < 1)
        {
            complain($"\"{item}\" is not Label=value.");
            return;
        }

        var label = item[..at].Trim();
        var settable = example.Settings().Where(IsValue).ToList();
        if (settable.FirstOrDefault(one => string.Equals(one.Label, label, StringComparison.OrdinalIgnoreCase)) is not { } found)
        {
            complain($"{example.Name} has no setting called \"{label}\". It has {string.Join(", ", settable.Select(one => $"\"{one.Label}\""))}.");
            return;
        }

        var (change, wrong) = Plan(found, item[(at + 1)..].Trim());
        if (change is null)
        {
            complain($"{found.Label}: {wrong}");
            return;
        }

        change();
    }

    // A caption is text between the controls and a row of buttons is an event, so neither holds a value.
    private static bool IsValue(ExampleSetting setting) => setting is Setting or Choice or Toggle or TextEntry;

    private static (Action? Change, string? Wrong) Plan(ExampleSetting setting, string value) => setting switch
    {
        Setting slider => Slide(slider, value),
        Choice { Select: { } select } choice => Pick(choice, select, value),
        Toggle { Enabled: true } toggle => Flip(toggle, value),
        TextEntry entry => (() => entry.Change(value), null),
        _ => (null, "it has nothing to act on yet."),
    };

    private static (Action? Change, string? Wrong) Slide(Setting slider, string value)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) || !double.IsFinite(number))
        {
            return (null, $"\"{value}\" is not a number.");
        }

        if (number < slider.Min || number > slider.Max)
        {
            return (null, string.Create(CultureInfo.InvariantCulture, $"it takes {slider.Min} to {slider.Max}."));
        }

        // Settle as well, because it is where a slider with an expensive answer does the work.
        return (() =>
        {
            slider.Change(number);
            slider.Settle?.Invoke(number);
        }, null);
    }

    private static (Action? Change, string? Wrong) Pick(Choice choice, Action<string> select, string value)
    {
        var option = choice.Options.FirstOrDefault(one => string.Equals(one, value, StringComparison.OrdinalIgnoreCase));
        return option is null
            ? (null, $"it is one of {string.Join(", ", choice.Options.Select(one => $"\"{one}\""))}.")
            : (() => select(option), null);
    }

    private static (Action? Change, string? Wrong) Flip(Toggle toggle, string value) =>
        value.ToLowerInvariant() switch
        {
            "1" or "on" or "true" or "yes" => (() => toggle.Change(true), null),
            "0" or "off" or "false" or "no" => (() => toggle.Change(false), null),
            _ => (null, $"\"{value}\" is not on or off."),
        };
}
