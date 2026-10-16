using AvaAdwaita.CssEngine.Selectors;

namespace AvaAdwaita.CssEngine.Cascade;

/// <summary>One selector of one rule in the index, with its cascade sort key.</summary>
/// <param name="Selector">The selector.</param>
/// <param name="Rule">The rule.</param>
/// <param name="SortKey">Origin, then sheet, then specificity, then source order.</param>
/// <param name="Sheet">The sheet.</param>
internal sealed record CssRuleEntry(CssSelector Selector, CssRule Rule, long SortKey, CssStyleSheet Sheet);
