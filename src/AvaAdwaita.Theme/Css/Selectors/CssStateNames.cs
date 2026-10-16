namespace AvaAdwaita.CssEngine.Selectors;

/// <summary>The CSS names of the state flags.</summary>
internal static class CssStateNames
{
    public static string Name(CssStates s) => s switch
    {
        CssStates.Active => "active",
        CssStates.Hover => "hover",
        CssStates.Selected => "selected",
        CssStates.Disabled => "disabled",
        CssStates.Indeterminate => "indeterminate",
        CssStates.Focus => "focus",
        CssStates.Backdrop => "backdrop",
        CssStates.DirLtr => "dir(ltr)",
        CssStates.DirRtl => "dir(rtl)",
        CssStates.Link => "link",
        CssStates.Visited => "visited",
        CssStates.Checked => "checked",
        CssStates.DropActive => "drop(active)",
        CssStates.FocusVisible => "focus-visible",
        CssStates.FocusWithin => "focus-within",
        _ => s.ToString(),
    };

    public static CssStates? FromName(string name) => name.ToLowerInvariant() switch
    {
        "active" => CssStates.Active,
        "hover" => CssStates.Hover,
        "selected" => CssStates.Selected,
        "disabled" => CssStates.Disabled,
        "indeterminate" => CssStates.Indeterminate,
        "focus" => CssStates.Focus,
        "backdrop" => CssStates.Backdrop,
        "link" => CssStates.Link,
        "visited" => CssStates.Visited,
        "checked" => CssStates.Checked,
        "focus-visible" => CssStates.FocusVisible,
        "focus-within" => CssStates.FocusWithin,
        _ => null,
    };

    /// <summary>Prints a state set as pseudo-classes.</summary>
    public static string Format(CssStates states)
    {
        var sb = new System.Text.StringBuilder();
        for (var bit = 0; bit < 15; bit++)
        {
            var f = (CssStates)(1u << bit);
            if ((states & f) != 0)
            {
                sb.Append(':').Append(Name(f));
            }
        }

        return sb.ToString();
    }
}
