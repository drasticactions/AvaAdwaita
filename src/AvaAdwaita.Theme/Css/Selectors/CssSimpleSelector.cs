using System;
using AvaAdwaita.Nodes;

namespace AvaAdwaita.CssEngine.Selectors;

/// <summary>One simple selector, possibly negated by <c>:not()</c>.</summary>
internal readonly record struct CssSimpleSelector(CssSimpleKind Kind, bool Negated, string? Text, CssStates State, int A, int B)
{
    public bool Matches(CssNode node)
    {
        var r = Kind switch
        {
            CssSimpleKind.Any => true,
            CssSimpleKind.Name => string.Equals(node.Name, Text, StringComparison.Ordinal),
            CssSimpleKind.Class => node.HasClass(Text!),
            CssSimpleKind.Id => string.Equals(node.Id, Text, StringComparison.Ordinal),
            CssSimpleKind.State => (node.States & State) == State,
            CssSimpleKind.PositionForward => MatchPosition(node.PositionFromStart, A, B),
            CssSimpleKind.PositionBackward => MatchPosition(node.PositionFromEnd, A, B),
            CssSimpleKind.PositionOnly => node.PositionFromStart == 1 && node.PositionFromEnd == 1,
            CssSimpleKind.Root => node.IsRoot,
            _ => false,
        };
        return r != Negated;
    }

    /// <summary>Port of match_position (gtkcssselector.c); <paramref name="pos"/> is 1-based.</summary>
    private static bool MatchPosition(int pos, int a, int b)
    {
        if (a == 0)
        {
            return pos == b;
        }

        var x = pos - b;
        if (x % a != 0)
        {
            return false;
        }

        return x / a >= 0;
    }

    public override string ToString()
    {
        var s = Kind switch
        {
            CssSimpleKind.Any => "*",
            CssSimpleKind.Name => Text!,
            CssSimpleKind.Class => "." + Text,
            CssSimpleKind.Id => "#" + Text,
            CssSimpleKind.State => ":" + CssStateNames.Name(State),
            CssSimpleKind.PositionForward => A == 0 && B == 1 ? ":first-child" : $":nth-child({A}n+{B})",
            CssSimpleKind.PositionBackward => A == 0 && B == 1 ? ":last-child" : $":nth-last-child({A}n+{B})",
            CssSimpleKind.PositionOnly => ":only-child",
            CssSimpleKind.Root => ":root",
            _ => "?",
        };
        return Negated ? ":not(" + s + ")" : s;
    }
}
