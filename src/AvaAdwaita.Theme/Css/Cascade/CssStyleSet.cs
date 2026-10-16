using System;
using System.Collections.Generic;
using AvaAdwaita.CssEngine.Selectors;

namespace AvaAdwaita.CssEngine.Cascade;

/// <summary>
/// The ordered style sheets of a cascade, as GTK's style cascade: by origin, then in the order added.
/// It compiles the active rules and named colors for each media state on demand.
/// </summary>
public sealed class CssStyleSet
{
    private readonly List<CssStyleSheet> _sheets = new();
    private readonly Dictionary<CssMediaState, CssCompiledStyles> _compiled = new();
    private readonly object _lock = new();

    /// <summary>Raised after a sheet is added or removed.</summary>
    public event EventHandler? Changed;

    /// <summary>The sheets in cascade order.</summary>
    public IReadOnlyList<CssStyleSheet> Sheets
    {
        get
        {
            lock (_lock)
            {
                return _sheets.ToArray();
            }
        }
    }

    /// <summary>Increments on every change.</summary>
    public int Version { get; private set; }

    /// <summary>Adds a sheet after the others of its origin.</summary>
    public void Add(CssStyleSheet sheet)
    {
        lock (_lock)
        {
            var i = _sheets.FindLastIndex(s => s.Origin <= sheet.Origin) + 1;
            _sheets.Insert(i, sheet);
            Invalidate();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Replaces <paramref name="old"/> with <paramref name="sheet"/> in place, or adds it.</summary>
    public void Replace(CssStyleSheet? old, CssStyleSheet sheet)
    {
        lock (_lock)
        {
            var i = old is null ? -1 : _sheets.IndexOf(old);
            if (i < 0 || old!.Origin != sheet.Origin)
            {
                if (i >= 0)
                {
                    _sheets.RemoveAt(i);
                }

                i = _sheets.FindLastIndex(s => s.Origin <= sheet.Origin) + 1;
                _sheets.Insert(i, sheet);
            }
            else
            {
                _sheets[i] = sheet;
            }

            Invalidate();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Removes a sheet.</summary>
    public bool Remove(CssStyleSheet sheet)
    {
        bool removed;
        lock (_lock)
        {
            removed = _sheets.Remove(sheet);
            if (removed)
            {
                Invalidate();
            }
        }

        if (removed)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }

        return removed;
    }

    private void Invalidate()
    {
        Version++;
        _compiled.Clear();
    }

    internal CssCompiledStyles Compile(CssMediaState state)
    {
        lock (_lock)
        {
            if (_compiled.TryGetValue(state, out var c))
            {
                return c;
            }

            c = new CssCompiledStyles(_sheets, state);
            _compiled[state] = c;
            return c;
        }
    }
}
