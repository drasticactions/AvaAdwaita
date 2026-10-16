using System.Collections.Generic;
using System.Text;
using AvaAdwaita.Nodes;

namespace AvaAdwaita.CssEngine.Selectors;

/// <summary>A complex selector: compounds joined by combinators, matched right to left.</summary>
internal sealed class CssSelector
{
    public CssSelector(CssCompound[] compounds, CssCombinator[] combinators)
    {
        Compounds = compounds;
        Combinators = combinators;
        var spec = new CssSpecificity(0, 0, 0);
        foreach (var c in compounds)
        {
            var s = c.Specificity();
            spec = new CssSpecificity(spec.Ids + s.Ids, spec.Classes + s.Classes, spec.Elements + s.Elements);
        }

        Specificity = spec;

        var keys = new List<int>();
        for (var i = 0; i < compounds.Length - 1; i++)
        {
            var comb = combinators[i];
            if (comb is CssCombinator.Adjacent or CssCombinator.Sibling)
            {
                UsesSiblings = true;
                continue;
            }

            // Compound i is an ancestor of the subject when the combinator right of it is a descendant or child one.
            var c = compounds[i];
            if (c.Name is not null)
            {
                keys.Add(CssBloom.NameKey(c.Name));
            }

            if (c.Id is not null)
            {
                keys.Add(CssBloom.IdKey(c.Id));
            }

            foreach (var cl in c.Classes)
            {
                keys.Add(CssBloom.ClassKey(cl));
            }
        }

        AncestorKeys = keys.ToArray();
        foreach (var c in compounds)
        {
            foreach (var p in c.Parts)
            {
                if (p.Kind is CssSimpleKind.PositionForward or CssSimpleKind.PositionBackward or CssSimpleKind.PositionOnly)
                {
                    UsesPositions = true;
                }
            }
        }
    }

    public CssCompound[] Compounds { get; }

    /// <summary><c>Combinators[i]</c> joins <c>Compounds[i]</c> and <c>Compounds[i + 1]</c>.</summary>
    public CssCombinator[] Combinators { get; }

    public CssSpecificity Specificity { get; }

    /// <summary>Bloom keys that some ancestor must have.</summary>
    public int[] AncestorKeys { get; }

    public bool UsesSiblings { get; }

    public bool UsesPositions { get; }

    public CssCompound Subject => Compounds[^1];

    public bool Matches(CssNode node) => MatchFrom(Compounds.Length - 1, node);

    private bool MatchFrom(int k, CssNode node)
    {
        if (!Compounds[k].Matches(node))
        {
            return false;
        }

        if (k == 0)
        {
            return true;
        }

        switch (Combinators[k - 1])
        {
            case CssCombinator.Child:
                return node.Parent is { } p && MatchFrom(k - 1, p);
            case CssCombinator.Descendant:
                for (var a = node.Parent; a is not null; a = a.Parent)
                {
                    if (MatchFrom(k - 1, a))
                    {
                        return true;
                    }
                }

                return false;
            case CssCombinator.Adjacent:
                return node.PreviousVisibleSibling is { } prev && MatchFrom(k - 1, prev);
            default:
                for (var s = node.PreviousVisibleSibling; s is not null; s = s.PreviousVisibleSibling)
                {
                    if (MatchFrom(k - 1, s))
                    {
                        return true;
                    }
                }

                return false;
        }
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        for (var i = 0; i < Compounds.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(Combinators[i - 1] switch
                {
                    CssCombinator.Child => " > ",
                    CssCombinator.Adjacent => " + ",
                    CssCombinator.Sibling => " ~ ",
                    _ => " ",
                });
            }

            sb.Append(Compounds[i]);
        }

        return sb.ToString();
    }
}
