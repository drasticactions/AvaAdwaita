using Avalonia;
using AvaAdwaita.CssEngine.Computed;
using AvaAdwaita.CssEngine.Selectors;

namespace AvaAdwaita.Nodes;

/// <summary>
/// Attached properties that make an element a GTK CSS node. The element's own classes are CSS classes,
/// and its pseudo-classes map to GTK state flags.
/// </summary>
public static class Css
{
    /// <summary>The node name, as <c>button</c>, <c>entry</c> or <c>trough</c>. Setting it makes the element a node.</summary>
    public static readonly AttachedProperty<string?> NodeProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, string?>("Node", typeof(Css));

    /// <summary>The node's <c>#id</c>.</summary>
    public static readonly AttachedProperty<string?> IdProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, string?>("Id", typeof(Css));

    /// <summary>Extra classes, separated by spaces, that a template adds to its parts.</summary>
    public static readonly AttachedProperty<string?> ClassesProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, string?>("Classes", typeof(Css));

    /// <summary>Where the node's states come from.</summary>
    public static readonly AttachedProperty<CssStateSource> StateSourceProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, CssStateSource>("StateSource", typeof(Css));

    /// <summary>Where the node's position among siblings comes from.</summary>
    public static readonly AttachedProperty<CssPositionSource> PositionProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, CssPositionSource>("Position", typeof(Css));

    /// <summary>
    /// States added to the node's own, for parts whose GTK state no Avalonia pseudo-class gives: a split button's
    /// arrow is <c>:checked</c> while the menu is open, a hyperlink is a <c>:link</c>. Set it from a style.
    /// </summary>
    public static readonly AttachedProperty<CssStates> StatesProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, CssStates>("States", typeof(Css));

    /// <summary>
    /// GTK's sensitivity for a subtree, when it differs from Avalonia's enabled state: nodes at and below take
    /// <c>:disabled</c> from it rather than from their own elements. For controls that disable children only to make
    /// them read-only, as a data grid's check boxes outside the current cell. It inherits; null uses the elements.
    /// </summary>
    public static readonly AttachedProperty<bool?> SensitiveProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool?>("Sensitive", typeof(Css), inherits: true);

    /// <summary>The computed style of the nearest node at or above the element. It inherits.</summary>
    public static readonly AttachedProperty<ComputedStyle?> StyleProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, ComputedStyle?>("Style", typeof(Css), inherits: true);

    internal static readonly AttachedProperty<NodeBinder?> BinderProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, NodeBinder?>("Binder", typeof(Css));

    static Css()
    {
        NodeProperty.Changed.AddClassHandler<StyledElement>((e, _) => NodeBinder.Update(e));
        IdProperty.Changed.AddClassHandler<StyledElement>((e, _) => GetBinder(e)?.Refresh());
        ClassesProperty.Changed.AddClassHandler<StyledElement>((e, _) => GetBinder(e)?.Refresh());
        StateSourceProperty.Changed.AddClassHandler<StyledElement>((e, _) => GetBinder(e)?.Rebind());
        PositionProperty.Changed.AddClassHandler<StyledElement>((e, _) => GetBinder(e)?.Rebind());
        StatesProperty.Changed.AddClassHandler<StyledElement>((e, _) => GetBinder(e)?.Refresh());
    }

    /// <summary>Gets <see cref="NodeProperty"/>.</summary>
    public static string? GetNode(StyledElement element) => element.GetValue(NodeProperty);

    /// <summary>Sets <see cref="NodeProperty"/>.</summary>
    public static void SetNode(StyledElement element, string? value) => element.SetValue(NodeProperty, value);

    /// <summary>Gets <see cref="IdProperty"/>.</summary>
    public static string? GetId(StyledElement element) => element.GetValue(IdProperty);

    /// <summary>Sets <see cref="IdProperty"/>.</summary>
    public static void SetId(StyledElement element, string? value) => element.SetValue(IdProperty, value);

    /// <summary>Gets <see cref="ClassesProperty"/>.</summary>
    public static string? GetClasses(StyledElement element) => element.GetValue(ClassesProperty);

    /// <summary>Sets <see cref="ClassesProperty"/>.</summary>
    public static void SetClasses(StyledElement element, string? value) => element.SetValue(ClassesProperty, value);

    /// <summary>Gets <see cref="StateSourceProperty"/>.</summary>
    public static CssStateSource GetStateSource(StyledElement element) => element.GetValue(StateSourceProperty);

    /// <summary>Sets <see cref="StateSourceProperty"/>.</summary>
    public static void SetStateSource(StyledElement element, CssStateSource value) => element.SetValue(StateSourceProperty, value);

    /// <summary>Gets <see cref="StatesProperty"/>.</summary>
    public static CssStates GetStates(StyledElement element) => element.GetValue(StatesProperty);

    /// <summary>Sets <see cref="StatesProperty"/>.</summary>
    public static void SetStates(StyledElement element, CssStates value) => element.SetValue(StatesProperty, value);

    /// <summary>Gets <see cref="PositionProperty"/>.</summary>
    public static CssPositionSource GetPosition(StyledElement element) => element.GetValue(PositionProperty);

    /// <summary>Sets <see cref="PositionProperty"/>.</summary>
    public static void SetPosition(StyledElement element, CssPositionSource value) => element.SetValue(PositionProperty, value);

    /// <summary>Gets <see cref="SensitiveProperty"/>.</summary>
    public static bool? GetSensitive(StyledElement element) => element.GetValue(SensitiveProperty);

    /// <summary>Sets <see cref="SensitiveProperty"/>.</summary>
    public static void SetSensitive(StyledElement element, bool? value) => element.SetValue(SensitiveProperty, value);

    /// <summary>Gets <see cref="StyleProperty"/>.</summary>
    public static ComputedStyle? GetStyle(StyledElement element) => element.GetValue(StyleProperty);

    /// <summary>The live CSS node of an element that has <see cref="NodeProperty"/>, or null.</summary>
    public static CssNode? GetCssNode(StyledElement element) => element.GetValue(BinderProperty)?.Node;

    internal static NodeBinder? GetBinder(StyledElement element) => element.GetValue(BinderProperty);

    internal static void EnsureRegistered()
    {
    }
}
