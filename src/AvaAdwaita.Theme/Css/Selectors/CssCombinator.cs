namespace AvaAdwaita.CssEngine.Selectors;

/// <summary>How two compound selectors relate.</summary>
internal enum CssCombinator : byte
{
    Descendant,
    Child,
    Adjacent,
    Sibling,
}
