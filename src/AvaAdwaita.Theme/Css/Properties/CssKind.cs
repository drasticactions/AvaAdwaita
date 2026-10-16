using AvaAdwaita.CssEngine.Syntax;

namespace AvaAdwaita.CssEngine.Properties;

/// <summary>Parses and computes the values of one kind of property.</summary>
internal abstract class CssKind
{
    /// <summary>Parses a whole declaration value. Returns null and records an error on the reader when it does not parse.</summary>
    public abstract object? Parse(CssReader r, CssParseContext pc);

    /// <summary>Prints a specified value as CSS that <see cref="Parse"/> reads back to an equal value.</summary>
    public virtual string Print(object specified) => CssSerializer.Print(specified);

    /// <summary>Computes a specified value.</summary>
    public virtual object Compute(object specified, CssComputeContext ctx) =>
        specified is ICssSpecified s ? s.Compute(ctx) : specified;

    /// <summary>True when the computed value depends on the node's <c>color</c>, so an inheriting child recomputes it.</summary>
    public virtual bool UsesCurrentColor(object specified) => specified is ICssSpecified { UsesCurrentColor: true };

    /// <summary>Interpolates two computed values for a transition, or returns null when they cannot be.</summary>
    public virtual object? Interpolate(object from, object to, double t) => null;
}
