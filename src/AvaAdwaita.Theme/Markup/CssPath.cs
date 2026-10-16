using System;
using AvaAdwaita.CssEngine.Computed;

namespace AvaAdwaita.Markup;

/// <summary>A path into <see cref="ComputedStyle"/> that <c>{adw:Css}</c> reads.</summary>
/// <param name="Name">The path.</param>
/// <param name="PropertyId">The property id, or -1 for a composite path.</param>
/// <param name="ValueType">The computed value's type.</param>
/// <param name="Get">Reads the value.</param>
internal sealed record CssPath(string Name, int PropertyId, Type ValueType, Func<ComputedStyle, object?> Get);
