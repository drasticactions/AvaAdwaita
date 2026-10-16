using System;
using System.Collections.Generic;
using AvaAdwaita.CssEngine.Values;

namespace AvaAdwaita.CssEngine.Cascade;

/// <summary>The rules, named colors and keyframes that apply for one media state.</summary>
internal sealed class CssCompiledStyles : ICssColorScope
{
    private readonly Dictionary<string, CssColorExpr> _colors = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CssKeyframes> _keyframes = new(StringComparer.Ordinal);

    public CssCompiledStyles(IReadOnlyList<CssStyleSheet> sheets, CssMediaState state)
    {
        State = state;
        Index = new CssRuleIndex();
        for (var s = 0; s < sheets.Count; s++)
        {
            var sheet = sheets[s];
            var rank = sheet.Origin switch
            {
                CssOrigin.Theme => 0L,
                CssOrigin.Application => 1L,
                _ => 2L,
            };

            // Named colors: later sheets and later definitions win, as the highest-priority provider does in GTK.
            foreach (var def in sheet.Colors)
            {
                if (def.Media is null || def.Media.Evaluate(state))
                {
                    _colors[def.Name] = def.Value;
                }
            }

            foreach (var k in sheet.Keyframes)
            {
                if (k.Media is null || k.Media.Evaluate(state))
                {
                    _keyframes[k.Name] = k;
                }
            }

            foreach (var rule in sheet.Rules)
            {
                if (rule.Media is not null && !rule.Media.Evaluate(state))
                {
                    continue;
                }

                foreach (var sel in rule.Selectors)
                {
                    var key = (rank << 60) | ((long)Math.Min(s, 255) << 52) | ((long)sel.Specificity.Packed << 20) | (long)Math.Min(rule.Order, (1 << 20) - 1);
                    Index.Add(new CssRuleEntry(sel, rule, key, sheet));
                }
            }
        }
    }

    public CssMediaState State { get; }

    public CssRuleIndex Index { get; }

    public CssColorExpr? LookupColor(string name) => _colors.GetValueOrDefault(name);

    public IEnumerable<string> ColorNames => _colors.Keys;

    public CssKeyframes? LookupKeyframes(string name) => _keyframes.GetValueOrDefault(name);
}
