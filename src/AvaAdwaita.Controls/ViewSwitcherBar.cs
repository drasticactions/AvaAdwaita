using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace AvaAdwaita.Controls;

/// <summary>
/// A narrow <see cref="ViewSwitcher"/> in an action bar (AdwViewSwitcherBar), for the bottom of a narrow window. It
/// slides up when <see cref="Reveal"/> turns on, usually when the header bar's switcher no longer fits.
/// </summary>
public class ViewSwitcherBar : TemplatedControl
{
    /// <summary>Defines <see cref="Stack"/>.</summary>
    public static readonly StyledProperty<ViewStack?> StackProperty =
        ViewSwitcher.StackProperty.AddOwner<ViewSwitcherBar>();

    /// <summary>Defines <see cref="Reveal"/>.</summary>
    public static readonly StyledProperty<bool> RevealProperty =
        AvaloniaProperty.Register<ViewSwitcherBar, bool>(nameof(Reveal));

    private System.IDisposable? _stackBinding;

    /// <summary>The stack whose pages the bar shows.</summary>
    public ViewStack? Stack
    {
        get => GetValue(StackProperty);
        set => SetValue(StackProperty, value);
    }

    /// <summary>Whether the bar shows.</summary>
    public bool Reveal
    {
        get => GetValue(RevealProperty);
        set => SetValue(RevealProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // The switcher is the action bar's center widget, where a template binding does not reach.
        _stackBinding?.Dispose();
        _stackBinding = e.NameScope.Find<ViewSwitcher>("PART_Switcher")?.Bind(ViewSwitcher.StackProperty, this.GetObservable(StackProperty));
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.ViewSwitcherBarAutomationPeer(this);
}
