using AvaAdwaita.CssEngine.Properties;
using AvaAdwaita.CssEngine.Syntax;

namespace AvaAdwaita.CssEngine.Cascade;

/// <summary>A shorthand expanded late: because its value uses <c>var()</c>, or because its sheet is parsed lazily.</summary>
internal sealed class CssPendingShorthand(CssShorthand shorthand, CssComponent[] raw, bool hasVar, CssParseContext context)
{
    private object?[]? _values;
    private bool _expanded;

    public CssShorthand Shorthand { get; } = shorthand;

    public CssComponent[] Raw { get; } = raw;

    public bool HasVar { get; } = hasVar;

    public CssParseContext Context { get; } = context;

    /// <summary>The longhand values of a shorthand without <c>var()</c>, or null when it does not parse.</summary>
    public object?[]? StaticValues
    {
        get
        {
            if (!_expanded)
            {
                _expanded = true;
                _values = Expand(Raw);
            }

            return _values;
        }
    }

    /// <summary>Parses <paramref name="value"/> as this shorthand. Null entries become initial values.</summary>
    public object?[]? Expand(CssComponent[] value)
    {
        var r = new CssReader(value);
        var values = Shorthand.Parser(r, Shorthand, Context);
        if (values is null || !r.AtEnd)
        {
            return null;
        }

        for (var k = 0; k < values.Length; k++)
        {
            values[k] ??= CssPropertyTable.All[Shorthand.Longhands[k]].Initial;
        }

        return values;
    }
}
