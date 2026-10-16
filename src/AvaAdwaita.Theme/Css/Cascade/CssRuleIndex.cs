using System;
using System.Collections.Generic;
using AvaAdwaita.CssEngine.Selectors;
using AvaAdwaita.Nodes;

namespace AvaAdwaita.CssEngine.Cascade;

/// <summary>
/// The active rules for one media state, bucketed by the most selective key of each selector's
/// rightmost compound: id, then a class, then the node name, else universal.
/// </summary>
internal sealed class CssRuleIndex
{
    private readonly Dictionary<string, List<Candidate>> _byId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<Candidate>> _byClass = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<Candidate>> _byName = new(StringComparer.Ordinal);
    private readonly List<Candidate> _universal = new();
    private readonly HashSet<string> _siblingNames = new(StringComparer.Ordinal);
    private readonly HashSet<string> _siblingClasses = new(StringComparer.Ordinal);
    private CssStates _siblingStates;
    private bool _siblingAny;

    public int EntryCount { get; private set; }

    public void Add(CssRuleEntry e)
    {
        EntryCount++;
        var subject = e.Selector.Subject;
        var candidate = new Candidate(e);
        if (subject.Id is { } id)
        {
            Bucket(_byId, id).Add(candidate);
        }
        else if (subject.Classes.Length > 0)
        {
            Bucket(_byClass, subject.Classes[0]).Add(candidate);
        }
        else if (subject.Name is { } name)
        {
            Bucket(_byName, name).Add(candidate);
        }
        else
        {
            _universal.Add(candidate);
        }

        var sel = e.Selector;
        for (var i = 0; i < sel.Combinators.Length; i++)
        {
            if (sel.Combinators[i] is not (CssCombinator.Adjacent or CssCombinator.Sibling))
            {
                continue;
            }

            var left = sel.Compounds[i];
            if (left.Name is null && left.Classes.Length == 0 && left.Id is null)
            {
                _siblingAny = true;
            }

            if (left.Name is { } n)
            {
                _siblingNames.Add(n);
            }

            foreach (var c in left.Classes)
            {
                _siblingClasses.Add(c);
            }

            _siblingStates |= left.States;
            foreach (var p in left.Parts)
            {
                if (p.Kind == CssSimpleKind.State)
                {
                    _siblingStates |= p.State;
                }

                if (p.Negated && p.Kind == CssSimpleKind.Class)
                {
                    _siblingClasses.Add(p.Text!);
                }
            }
        }
    }

    private static List<Candidate> Bucket(Dictionary<string, List<Candidate>> d, string key)
    {
        if (!d.TryGetValue(key, out var list))
        {
            list = new List<Candidate>();
            d[key] = list;
        }

        return list;
    }

    /// <summary>True when a change on <paramref name="node"/> can change how its following siblings match.</summary>
    public bool SiblingsDependOn(CssNode node, CssNodeChange change, CssStates states)
    {
        if (_siblingAny)
        {
            return true;
        }

        switch (change)
        {
            case CssNodeChange.State:
                return (states & _siblingStates) != 0 && (node.Name is not null && _siblingNames.Contains(node.Name) || HasAny(node));
            case CssNodeChange.Classes:
            case CssNodeChange.Name:
                return true;
            default:
                return false;
        }
    }

    private bool HasAny(CssNode node)
    {
        foreach (var c in node.Classes)
        {
            if (_siblingClasses.Contains(c))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Collects the entries whose selectors match <paramref name="node"/>, in cascade order.</summary>
    public void Match(CssNode node, List<CssRuleEntry> result)
    {
        result.Clear();
        if (node.Id is { } id && _byId.TryGetValue(id, out var ids))
        {
            Test(ids, node, result);
        }

        foreach (var c in node.Classes)
        {
            if (_byClass.TryGetValue(c, out var list))
            {
                Test(list, node, result);
            }
        }

        if (node.Name is { } name && _byName.TryGetValue(name, out var names))
        {
            Test(names, node, result);
        }

        Test(_universal, node, result);
        result.Sort(static (a, b) => a.SortKey.CompareTo(b.SortKey));
    }

    private static void Test(List<Candidate> list, CssNode node, List<CssRuleEntry> result)
    {
        var bloom = node.AncestorBloom;
        var states = node.States;
        var name = node.Name;
        foreach (ref readonly var c in System.Runtime.InteropServices.CollectionsMarshal.AsSpan(list))
        {
            // Most candidates fail on the subject's states or name, or on an ancestor the filter lacks; those
            // checks read only the candidate, before the selector is walked.
            if ((states & c.States) != c.States
                || (c.Name is not null && !string.Equals(c.Name, name, StringComparison.Ordinal))
                || !bloom.MayContainAll(c.Ancestors)
                || !HasClasses(node, c.Classes)
                || !c.Entry.Selector.Matches(node))
            {
                continue;
            }

            result.Add(c.Entry);
        }
    }

    private static bool HasClasses(CssNode node, string[] classes)
    {
        foreach (var c in classes)
        {
            if (!node.HasClass(c))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>An entry with what its subject needs and its ancestors' filter mask, read without following the selector.</summary>
    private readonly struct Candidate(CssRuleEntry entry)
    {
        public readonly CssRuleEntry Entry = entry;
        public readonly CssStates States = entry.Selector.Subject.States;
        public readonly string? Name = entry.Selector.Subject.Name;
        public readonly string[] Classes = entry.Selector.Subject.Classes;
        public readonly CssBloom Ancestors = CssBloom.Of(entry.Selector.AncestorKeys);
    }
}
