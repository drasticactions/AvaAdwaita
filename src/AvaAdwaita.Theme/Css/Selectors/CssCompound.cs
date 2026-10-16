using System.Collections.Generic;
using System.Text;
using AvaAdwaita.Nodes;

namespace AvaAdwaita.CssEngine.Selectors;

/// <summary>A compound selector: simple selectors with no combinator between them.</summary>
internal sealed class CssCompound
{
    public CssCompound(CssSimpleSelector[] parts)
    {
        Parts = parts;
        var classes = new List<string>();
        foreach (var p in parts)
        {
            if (p.Negated)
            {
                continue;
            }

            switch (p.Kind)
            {
                case CssSimpleKind.Name:
                    Name = p.Text;
                    break;
                case CssSimpleKind.Id:
                    Id = p.Text;
                    break;
                case CssSimpleKind.Class:
                    classes.Add(p.Text!);
                    break;
                case CssSimpleKind.State:
                    States |= p.State;
                    break;
            }
        }

        Classes = classes.ToArray();
    }

    public CssSimpleSelector[] Parts { get; }

    /// <summary>The required node name, if any.</summary>
    public string? Name { get; }

    public string? Id { get; }

    public string[] Classes { get; }

    /// <summary>The states the compound requires.</summary>
    public CssStates States { get; }

    public bool Matches(CssNode node)
    {
        foreach (var p in Parts)
        {
            if (!p.Matches(node))
            {
                return false;
            }
        }

        return true;
    }

    public CssSpecificity Specificity()
    {
        int ids = 0, classes = 0, elements = 0;
        foreach (var p in Parts)
        {
            switch (p.Kind)
            {
                case CssSimpleKind.Name:
                    elements++;
                    break;
                case CssSimpleKind.Id:
                    ids++;
                    break;
                case CssSimpleKind.Any:
                    break;
                default:
                    classes++;
                    break;
            }
        }

        return new CssSpecificity(ids, classes, elements);
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var p in Parts)
        {
            sb.Append(p.ToString());
        }

        return sb.ToString();
    }
}
