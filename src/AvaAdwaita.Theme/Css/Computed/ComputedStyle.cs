using System;
using System.Collections.Generic;
using System.Text;
using AvaAdwaita.CssEngine.Properties;

namespace AvaAdwaita.CssEngine.Computed;

/// <summary>
/// The computed style of one CSS node: every supported property's computed value. Colors are resolved,
/// lengths are px and <c>var()</c> is substituted. Styles are interned, so equal styles are the same object.
/// </summary>
public sealed partial class ComputedStyle : IEquatable<ComputedStyle>
{
    private int? _hash;

    /// <summary>The computed custom properties.</summary>
    public CssCustomProperties Custom { get; }

    /// <summary>
    /// Inherited properties whose computed value depends on <c>color</c>, with their specified values,
    /// so a child that inherits them recomputes them with its own color, as GTK keeps <c>currentColor</c>.
    /// </summary>
    internal Dictionary<int, object>? CurrentColorDependents { get; init; }

    /// <summary>The key in <see cref="CurrentColorDependents"/> of a declared <c>color</c> that uses currentColor.</summary>
    internal const int SymbolicColorKey = -1;

    /// <summary>The key in <see cref="CurrentColorDependents"/> of <see cref="IconSourceColor"/>.</summary>
    internal const int IconSourceColorKey = -2;

    /// <summary>The key in <see cref="CurrentColorDependents"/> of <see cref="ColorMatrix"/>.</summary>
    internal const int ColorMatrixKey = -3;

    /// <summary>The color filters on the node and its ancestors, which change every color it draws; null for none.</summary>
    internal CssEngine.Values.CssColorMatrix? ColorMatrix =>
        CurrentColorDependents is { } d && d.TryGetValue(ColorMatrixKey, out var m) ? (CssEngine.Values.CssColorMatrix)m : null;

    /// <summary>A color as the node draws it, through its color filters.</summary>
    internal CssEngine.Values.CssColor Drawn(CssEngine.Values.CssColor c) => ColorMatrix is { } m ? m.Apply(c) : c;

    /// <summary>
    /// The color GTK recolors a <c>-gtk-icon-source</c> image with when it differs from <c>color</c>: an inherited
    /// color-mix() over currentColor resolved once more against the node's own color. Null when it is <c>color</c>.
    /// </summary>
    internal CssEngine.Values.CssColor? IconSourceColor =>
        CurrentColorDependents is { } d && d.TryGetValue(IconSourceColorKey, out var c) ? (CssEngine.Values.CssColor)c : null;

    /// <summary>The computed value of a property as CSS-like text, or null for an unknown property.</summary>
    public string? GetValueText(string property) =>
        CssPropertyTable.Lookup(property) is { } p ? Format(GetValue(p.Id)) : Custom.Get(property);

    internal static string Format(object? v) => v switch
    {
        null => "none",
        double d => d.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
        _ => v.ToString() ?? string.Empty,
    };

    /// <inheritdoc/>
    public bool Equals(ComputedStyle? other) =>
        other is not null && (ReferenceEquals(this, other) || (GroupsEqual(other) && Custom.Equals(other.Custom) && DependentsEqual(other)));

    private bool DependentsEqual(ComputedStyle other)
    {
        var a = CurrentColorDependents;
        var b = other.CurrentColorDependents;
        if (a is null || b is null)
        {
            return a is null && b is null;
        }

        if (a.Count != b.Count)
        {
            return false;
        }

        foreach (var (k, v) in a)
        {
            if (!b.TryGetValue(k, out var ov) || !(ReferenceEquals(v, ov) || (v is CssEngine.Values.CssColor or CssEngine.Values.CssColorMatrix && v.Equals(ov))))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ComputedStyle s && Equals(s);

    /// <inheritdoc/>
    public override int GetHashCode() => _hash ??= HashCode.Combine(GroupsHash(), Custom.GetHashCode(), CurrentColorDependents?.Count ?? 0);

    /// <summary>Lists every property whose computed value differs from <paramref name="baseline"/>.</summary>
    public string Describe(ComputedStyle? baseline = null)
    {
        var sb = new StringBuilder();
        foreach (var p in CssPropertyTable.All)
        {
            var v = GetValue(p.Id);
            if (baseline is not null && Equals(v, baseline.GetValue(p.Id)))
            {
                continue;
            }

            sb.Append(p.Name).Append(": ").Append(Format(v)).Append(";\n");
        }

        return sb.ToString();
    }
}
