namespace AvaAdwaita.CssEngine.Values;

/// <summary>Looks up <c>@define-color</c> names.</summary>
internal interface ICssColorScope
{
    /// <summary>The definition of <paramref name="name"/> that applies, or null.</summary>
    CssColorExpr? LookupColor(string name);
}
