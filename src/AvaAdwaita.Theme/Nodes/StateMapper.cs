using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using AvaAdwaita.CssEngine.Selectors;

namespace AvaAdwaita.Nodes;

/// <summary>Maps Avalonia pseudo-classes to GTK state flags.</summary>
public static class StateMapper
{
    /// <summary>The element that supplies <paramref name="element"/>'s states, or null for none.</summary>
    public static StyledElement? StateSourceOf(StyledElement element) => element.GetValue(Css.StateSourceProperty) switch
    {
        CssStateSource.Self => element,
        CssStateSource.TemplatedParent => element.TemplatedParent as StyledElement,
        CssStateSource.None => null,
        _ => element is TemplatedControl || element.TemplatedParent is not StyledElement ? element : element.TemplatedParent as StyledElement,
    };

    /// <summary>The GTK states of a node owned by <paramref name="owner"/> whose states come from <paramref name="source"/>.</summary>
    public static CssStates Map(StyledElement owner, StyledElement? source)
    {
        var states = CssStates.None;

        // Direction comes from the node's own element.
        states |= owner is Visual { FlowDirection: FlowDirection.RightToLeft } ? CssStates.DirRtl : CssStates.DirLtr;

        // GTK marks a whole insensitive subtree :disabled, as Avalonia's effective enabled state does.
        var sensitive = owner.GetValue(Css.SensitiveProperty);
        if (sensitive is { } s ? !s : owner is InputElement { IsEffectivelyEnabled: false } || source is InputElement { IsEffectivelyEnabled: false })
        {
            states |= CssStates.Disabled;
        }

        if (source is null)
        {
            return states;
        }

        foreach (var c in source.Classes)
        {
            if (c.Length == 0 || c[0] != ':')
            {
                continue;
            }

            if (CssNodeProviders.MapPseudoClass(source, owner, c) is { } mapped)
            {
                states |= mapped;
                continue;
            }

            switch (c)
            {
                case ":pointerover":
                    states |= CssStates.Hover;
                    break;
                case ":pressed":
                    states |= CssStates.Active;
                    break;
                case ":checked":
                    states |= CssStates.Checked;
                    break;
                case ":indeterminate":
                    states |= CssStates.Indeterminate;
                    break;
                case ":disabled":
                    states |= sensitive == true ? CssStates.None : CssStates.Disabled;
                    break;
                case ":focus":
                    states |= CssStates.Focus;
                    break;
                case ":focus-visible":
                    states |= CssStates.FocusVisible | CssStates.Focus;
                    break;
                case ":focus-within":
                    states |= CssStates.FocusWithin;
                    break;
                case ":selected":
                    states |= CssStates.Selected;
                    break;
                case ":visited":
                    states |= CssStates.Visited | CssStates.Link;
                    break;
                case ":drop-active":
                    states |= CssStates.DropActive;
                    break;
            }
        }

        // GTK's context menu is a popover inside the widget, so the widget keeps :focus-within while the menu has the
        // focus; Avalonia's opens in a popup of its own.
        if (source is Control { ContextMenu.IsOpen: true })
        {
            states |= CssStates.FocusWithin;
        }

        return states;
    }
}
