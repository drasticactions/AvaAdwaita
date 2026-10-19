using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Automation.Peers;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls;

/// <summary>
/// A row that opens a link (AdwLinkRow, which about dialogs use): an activatable action row with the external link
/// icon. Activating it raises <see cref="LinkActivated"/>, then opens <see cref="Uri"/> unless that was handled, and
/// marks it visited.
/// </summary>
public class LinkRow : ActionRow
{
    /// <summary>Defines <see cref="Uri"/>.</summary>
    public static readonly StyledProperty<string?> UriProperty =
        AvaloniaProperty.Register<LinkRow, string?>(nameof(Uri));

    /// <summary>Defines <see cref="IsVisited"/>.</summary>
    public static readonly StyledProperty<bool> IsVisitedProperty =
        AvaloniaProperty.Register<LinkRow, bool>(nameof(IsVisited));

    static LinkRow()
    {
        CssNodeProviders.Register<LinkRow>(_ => ["link"]);
        CssNodeProviders.MapPseudoClass<LinkRow>(":link", AvaAdwaita.CssEngine.Selectors.CssStates.Link);
        IsActivatableProperty.OverrideDefaultValue<LinkRow>(true);
    }

    /// <summary>Creates a link row.</summary>
    public LinkRow()
    {
        var icon = new AdwIcon { IconName = "adw-external-link-symbolic" };
        Css.SetNode(icon, "image");
        Suffixes.Add(icon);
        PseudoClasses.Set(":link", true);
    }

    /// <summary>Raised when the row is activated, with its URI; handle it to keep the row from opening the URI.</summary>
    public event EventHandler<LinkActivatedEventArgs>? LinkActivated;

    /// <summary>The link the row opens.</summary>
    public string? Uri
    {
        get => GetValue(UriProperty);
        set => SetValue(UriProperty, value);
    }

    /// <summary>Whether the link has been opened.</summary>
    public bool IsVisited
    {
        get => GetValue(IsVisitedProperty);
        set => SetValue(IsVisitedProperty, value);
    }

    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(ActionRow);

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsVisitedProperty)
        {
            // adw_link_row_set_visited: :visited instead of :link.
            PseudoClasses.Set(":visited", IsVisited);
            PseudoClasses.Set(":link", !IsVisited);
        }
    }

    /// <inheritdoc/>
    protected override void OnActivated()
    {
        base.OnActivated();
        var args = new LinkActivatedEventArgs(Uri ?? string.Empty);
        LinkActivated?.Invoke(this, args);
        if (!args.Handled && System.Uri.TryCreate(Uri, UriKind.Absolute, out var uri))
        {
            _ = Avalonia.Controls.TopLevel.GetTopLevel(this)?.Launcher.LaunchUriAsync(uri);
        }

        SetCurrentValue(IsVisitedProperty, true);
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.LinkRowAutomationPeer(this);
}
