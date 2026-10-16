namespace AvaAdwaita.CssEngine.Properties;

/// <summary>A specified value that needs the node's context to compute.</summary>
internal interface ICssSpecified
{
    /// <summary>True when the computed value depends on the node's <c>color</c>.</summary>
    bool UsesCurrentColor { get; }

    object Compute(CssComputeContext ctx);
}
