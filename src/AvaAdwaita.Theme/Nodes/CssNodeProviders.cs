using System;
using System.Collections.Generic;
using Avalonia;
using AvaAdwaita.CssEngine.Selectors;

namespace AvaAdwaita.Nodes;

/// <summary>
/// Describes controls to the node model: classes a control adds from its properties, and
/// pseudo-classes that mean a GTK state. Control libraries register their own controls here.
/// </summary>
public static class CssNodeProviders
{
    private static readonly List<(Type Type, Func<StyledElement, IEnumerable<string>> Classes, AvaloniaProperty[] Watched)> s_classProviders = new();
    private static readonly List<(Type Type, string PseudoClass, CssStates States, Func<StyledElement, StyledElement, bool>? When)> s_pseudo = new();
    private static readonly List<(Type Type, Func<StyledElement, (int Index, int Count)?> Position, AvaloniaProperty[] Watched)> s_positions = new();

    /// <summary>Adds classes to every node owned by a <typeparamref name="T"/>, recomputed when a watched property changes.</summary>
    public static void Register<T>(Func<T, IEnumerable<string>> classes, params AvaloniaProperty[] watchedProperties)
        where T : StyledElement =>
        s_classProviders.Add((typeof(T), e => classes((T)e), watchedProperties));

    /// <summary>
    /// Gives a <typeparamref name="T"/>'s node its position among its siblings, for controls whose children are not
    /// in display order (a data grid's rows and cells); recomputed when a watched property changes.
    /// </summary>
    public static void RegisterPosition<T>(Func<T, (int Index, int Count)?> position, params AvaloniaProperty[] watchedProperties)
        where T : StyledElement =>
        s_positions.Add((typeof(T), e => position((T)e), watchedProperties));

    /// <summary>Maps a pseudo-class of a <typeparamref name="T"/> state source to GTK states.</summary>
    public static void MapPseudoClass<T>(string pseudoClass, CssStates states)
        where T : StyledElement =>
        s_pseudo.Add((typeof(T), pseudoClass, states, null));

    /// <summary>Maps a pseudo-class of a <typeparamref name="T"/> state source to GTK states for some nodes only.</summary>
    /// <param name="pseudoClass">The pseudo-class.</param>
    /// <param name="states">The GTK states it sets.</param>
    /// <param name="when">Given the source and the node's owner, whether the mapping applies.</param>
    public static void MapPseudoClass<T>(string pseudoClass, CssStates states, Func<T, StyledElement, bool> when)
        where T : StyledElement =>
        s_pseudo.Add((typeof(T), pseudoClass, states, (s, o) => when((T)s, o)));

    internal static void AddClasses(StyledElement owner, List<string> classes)
    {
        foreach (var (type, provider, _) in s_classProviders)
        {
            if (type.IsInstanceOfType(owner))
            {
                foreach (var c in provider(owner))
                {
                    if (!classes.Contains(c))
                    {
                        classes.Add(c);
                    }
                }
            }
        }
    }

    internal static bool IsWatched(StyledElement owner, AvaloniaProperty property)
    {
        foreach (var (type, _, watched) in s_classProviders)
        {
            if (type.IsInstanceOfType(owner) && Array.IndexOf(watched, property) >= 0)
            {
                return true;
            }
        }

        foreach (var (type, _, watched) in s_positions)
        {
            if (type.IsInstanceOfType(owner) && Array.IndexOf(watched, property) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The node's position from a registered provider, or null.</summary>
    internal static (int Index, int Count)? Position(StyledElement owner)
    {
        foreach (var (type, position, _) in s_positions)
        {
            if (type.IsInstanceOfType(owner) && position(owner) is { } p)
            {
                return p;
            }
        }

        return null;
    }

    /// <summary>The states a pseudo-class maps to on this source, or null when no provider maps it.</summary>
    internal static CssStates? MapPseudoClass(StyledElement source, StyledElement owner, string pseudoClass)
    {
        CssStates? result = null;
        foreach (var (type, pc, states, when) in s_pseudo)
        {
            if (pc == pseudoClass && type.IsInstanceOfType(source) && (when is null || when(source, owner)))
            {
                result = (result ?? CssStates.None) | states;
            }
        }

        return result;
    }
}
