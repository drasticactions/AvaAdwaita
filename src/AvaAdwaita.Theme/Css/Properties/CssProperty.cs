using System;
using System.Collections.Generic;
using AvaAdwaita.CssEngine.Syntax;

namespace AvaAdwaita.CssEngine.Properties;

/// <summary>One longhand property: its id, name, kind, initial value and flags.</summary>
internal sealed class CssProperty
{
    private object? _initial;

    public CssProperty(int id, string name, CssKind kind, string initial, bool inherited, bool animated)
    {
        Id = id;
        Name = name;
        Kind = kind;
        InitialText = initial;
        Inherited = inherited;
        Animated = animated;
    }

    public int Id { get; }

    public string Name { get; }

    public CssKind Kind { get; }

    public string InitialText { get; }

    public bool Inherited { get; }

    public bool Animated { get; }

    /// <summary>The initial value as a specified value.</summary>
    public object Initial => _initial ??= ParseInitial();

    private object ParseInitial()
    {
        var r = new CssReader(CssComponentParser.Parse(InitialText));
        var v = Kind.Parse(r, CssParseContext.Default);
        if (v is null || !r.AtEnd)
        {
            throw new InvalidOperationException($"bad initial value '{InitialText}' for {Name}: {r.Error}");
        }

        return v;
    }

    public override string ToString() => Name;
}
