using System;
using System.Collections.Generic;
using AvaAdwaita.CssEngine.Selectors;

namespace AvaAdwaita.CssEngine.Cascade;

/// <summary>A style rule: selectors, declarations and the <c>@media</c> condition around it.</summary>
internal sealed class CssRule
{
    private CssDeclaration[]? _declarations;
    private Func<CssDeclaration[]>? _parse;

    public required List<CssSelector> Selectors { get; init; }

    /// <summary>The declarations. In a lazily parsed sheet they are parsed when first read.</summary>
    public CssDeclaration[] Declarations
    {
        get
        {
            if (_declarations is null)
            {
                _declarations = _parse!();
                _parse = null;
            }

            return _declarations;
        }

        init => _declarations = value;
    }

    /// <summary>Parses the declarations on first use instead.</summary>
    public Func<CssDeclaration[]> ParseDeclarations
    {
        init => _parse = value;
    }

    public CssMediaQuery? Media { get; init; }

    /// <summary>The rule's position in its sheet.</summary>
    public required int Order { get; init; }

    public required int Line { get; init; }

    public override string ToString() => string.Join(", ", Selectors);
}
