using AvaAdwaita.CssEngine.Properties;
using AvaAdwaita.CssEngine.Syntax;

namespace AvaAdwaita.CssEngine.Cascade;

/// <summary>One longhand or custom property declaration, after shorthand expansion.</summary>
/// <remarks>
/// In a lazily parsed sheet the value is parsed on first use. Only sheets already known to parse
/// cleanly are read lazily, so a value never turns out invalid late.
/// </remarks>
internal sealed class CssDeclaration
{
    private object? _value;
    private bool _parsed;

    /// <summary>The longhand id, or -1 for a custom property.</summary>
    public required int PropertyId { get; init; }

    /// <summary>The custom property name, for a custom property.</summary>
    public string? CustomName { get; init; }

    /// <summary>
    /// The specified value or a <see cref="CssWideKeyword"/>. Null when the value has <c>var()</c>,
    /// comes from a shorthand, or is a custom property.
    /// </summary>
    public object? Value
    {
        get
        {
            if (!_parsed)
            {
                ParseLazily();
            }

            return _value;
        }

        init
        {
            _value = value;
            _parsed = true;
        }
    }

    /// <summary>The raw value, for custom properties, values with <c>var()</c> and lazily parsed values.</summary>
    public CssComponent[]? Raw { get; init; }

    /// <summary>True when <see cref="Raw"/> contains <c>var()</c>.</summary>
    public bool HasVar { get; init; }

    /// <summary>Set when this longhand comes from a shorthand that is expanded late.</summary>
    public CssPendingShorthand? Pending { get; init; }

    /// <summary>The index of this longhand within <see cref="Pending"/>'s shorthand.</summary>
    public int PendingIndex { get; init; }

    /// <summary>How to resolve URLs when the value is parsed late.</summary>
    public CssParseContext? Context { get; init; }

    public int Line { get; init; }

    public bool IsCustom => PropertyId < 0;

    private void ParseLazily()
    {
        _parsed = true;
        if (IsCustom || HasVar || Pending is not null || Raw is null)
        {
            return;
        }

        var r = new CssReader(Raw);
        var v = CssPropertyTable.All[PropertyId].Kind.Parse(r, Context ?? CssParseContext.Default);
        _value = v is not null && r.AtEnd ? v : null;
    }
}
