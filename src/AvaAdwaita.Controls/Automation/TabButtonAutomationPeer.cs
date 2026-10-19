using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Interactivity;

namespace AvaAdwaita.Controls.Automation;

/// <summary>A tab button: a button named by its tooltip.</summary>
public class TabButtonAutomationPeer : ControlAutomationPeer, IInvokeProvider
{
    /// <summary>Creates the peer.</summary>
    public TabButtonAutomationPeer(TabButton owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    public void Invoke()
    {
        var owner = (TabButton)Owner;
        owner.RaiseEvent(new RoutedEventArgs(TabButton.ClickEvent, owner));
        if (owner.Command is { } command && command.CanExecute(owner.CommandParameter))
        {
            command.Execute(owner.CommandParameter);
        }
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

    /// <inheritdoc/>
    protected override string? GetNameCore() => base.GetNameCore() is { Length: > 0 } name ? name
        : ((TabButton)Owner).Tip;
}
