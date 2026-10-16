using System.Collections.Generic;

namespace AvaAdwaita.CssEngine.Computed;

/// <summary>Interns computed styles and their groups, so equal styles are the same object.</summary>
internal sealed class CssStyleInterner
{
    private readonly Dictionary<object, object> _table = new();
    private readonly object _lock = new();

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _table.Count;
            }
        }
    }

    public T Intern<T>(T value)
        where T : class
    {
        lock (_lock)
        {
            if (_table.TryGetValue(value, out var existing))
            {
                return (T)existing;
            }

            _table[value] = value;
            return value;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _table.Clear();
        }
    }
}
