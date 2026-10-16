using AvaAdwaita.CssEngine.Syntax;

namespace AvaAdwaita.CssEngine.Properties;

/// <summary>A shorthand property and its longhands.</summary>
internal sealed class CssShorthand
{
    public CssShorthand(string name, int[] longhands, CssShorthandParser parser)
    {
        Name = name;
        Longhands = longhands;
        Parser = parser;
    }

    public string Name { get; }

    public int[] Longhands { get; }

    public CssShorthandParser Parser { get; }

    public CssProperty Longhand(int i) => CssPropertyTable.All[Longhands[i]];

    public override string ToString() => Name;
}
