namespace AvaAdwaita.CssEngine.Syntax;

/// <summary>How a parse problem is classified.</summary>
public enum CssErrorKind
{
    /// <summary>Malformed CSS.</summary>
    Syntax,

    /// <summary>A property the engine does not know.</summary>
    UnknownProperty,

    /// <summary>A function the engine does not know.</summary>
    UnknownFunction,

    /// <summary>An at-rule the engine does not know.</summary>
    UnknownAtRule,

    /// <summary>A pseudo-class the engine does not know.</summary>
    UnknownPseudoClass,

    /// <summary>A value that does not parse for its property.</summary>
    BadValue,

    /// <summary>Accepted, but with a warning (deprecated or ignored syntax).</summary>
    Warning,
}
