using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls;

/// <summary>
/// A box that wraps its children into lines (AdwWrapBox): as many as fit on a line, then the next, the lines placed by
/// <see cref="Align"/> or filled by <see cref="Justify"/>. Children marked with <see cref="AdwStack.ExpandProperty"/> take
/// a line's extra space. Avalonia gives one size, so the minimum size is the natural one.
/// </summary>
public class WrapBox : Panel
{
    /// <summary>Defines <see cref="Orientation"/>.</summary>
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<WrapBox, Orientation>(nameof(Orientation));

    /// <summary>Defines <see cref="ChildSpacing"/>.</summary>
    public static readonly StyledProperty<double> ChildSpacingProperty =
        AvaloniaProperty.Register<WrapBox, double>(nameof(ChildSpacing));

    /// <summary>Defines <see cref="LineSpacing"/>.</summary>
    public static readonly StyledProperty<double> LineSpacingProperty =
        AvaloniaProperty.Register<WrapBox, double>(nameof(LineSpacing));

    /// <summary>Defines <see cref="Align"/>.</summary>
    public static readonly StyledProperty<double> AlignProperty =
        AvaloniaProperty.Register<WrapBox, double>(nameof(Align), coerce: (_, v) => Math.Clamp(v, 0, 1));

    /// <summary>Defines <see cref="Justify"/>.</summary>
    public static readonly StyledProperty<JustifyMode> JustifyProperty =
        AvaloniaProperty.Register<WrapBox, JustifyMode>(nameof(Justify));

    /// <summary>Defines <see cref="JustifyLastLine"/>.</summary>
    public static readonly StyledProperty<bool> JustifyLastLineProperty =
        AvaloniaProperty.Register<WrapBox, bool>(nameof(JustifyLastLine));

    /// <summary>Defines <see cref="LineHomogeneous"/>.</summary>
    public static readonly StyledProperty<bool> LineHomogeneousProperty =
        AvaloniaProperty.Register<WrapBox, bool>(nameof(LineHomogeneous));

    /// <summary>Defines <see cref="NaturalLineLength"/>.</summary>
    public static readonly StyledProperty<double> NaturalLineLengthProperty =
        AvaloniaProperty.Register<WrapBox, double>(nameof(NaturalLineLength), -1);

    /// <summary>Defines <see cref="WrapReverse"/>.</summary>
    public static readonly StyledProperty<bool> WrapReverseProperty =
        AvaloniaProperty.Register<WrapBox, bool>(nameof(WrapReverse));

    /// <summary>Defines <see cref="PackDirection"/>.</summary>
    public static readonly StyledProperty<PackDirection> PackDirectionProperty =
        AvaloniaProperty.Register<WrapBox, PackDirection>(nameof(PackDirection));

    /// <summary>Defines <see cref="WrapPolicy"/>.</summary>
    public static readonly StyledProperty<WrapPolicy> WrapPolicyProperty =
        AvaloniaProperty.Register<WrapBox, WrapPolicy>(nameof(WrapPolicy), WrapPolicy.Natural);

    static WrapBox()
    {
        AffectsMeasure<WrapBox>(OrientationProperty, ChildSpacingProperty, LineSpacingProperty, NaturalLineLengthProperty, LineHomogeneousProperty, WrapPolicyProperty);
        AffectsParentArrange<WrapBox>(AdwStack.ExpandProperty);
        AffectsArrange<WrapBox>(AlignProperty, JustifyProperty, JustifyLastLineProperty, WrapReverseProperty, PackDirectionProperty);
    }

    /// <summary>Creates a wrap box: a <c>wrap-box</c> node.</summary>
    public WrapBox()
    {
        SetValue(Css.NodeProperty, "wrap-box", BindingPriority.Style);
    }

    /// <summary>The direction of the lines: horizontal lines stack downwards.</summary>
    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>The space between children on a line.</summary>
    public double ChildSpacing
    {
        get => GetValue(ChildSpacingProperty);
        set => SetValue(ChildSpacingProperty, value);
    }

    /// <summary>The space between lines.</summary>
    public double LineSpacing
    {
        get => GetValue(LineSpacingProperty);
        set => SetValue(LineSpacingProperty, value);
    }

    /// <summary>Where a line's children sit when they do not fill it, from 0 (the start) to 1 (the end).</summary>
    public double Align
    {
        get => GetValue(AlignProperty);
        set => SetValue(AlignProperty, value);
    }

    /// <summary>How the lines fill their length.</summary>
    public JustifyMode Justify
    {
        get => GetValue(JustifyProperty);
        set => SetValue(JustifyProperty, value);
    }

    /// <summary>Whether the last line is justified too.</summary>
    public bool JustifyLastLine
    {
        get => GetValue(JustifyLastLineProperty);
        set => SetValue(JustifyLastLineProperty, value);
    }

    /// <summary>Whether every line is as tall as the tallest.</summary>
    public bool LineHomogeneous
    {
        get => GetValue(LineHomogeneousProperty);
        set => SetValue(LineHomogeneousProperty, value);
    }

    /// <summary>The box's natural length; -1 for all the children on one line.</summary>
    public double NaturalLineLength
    {
        get => GetValue(NaturalLineLengthProperty);
        set => SetValue(NaturalLineLengthProperty, value);
    }

    /// <summary>Whether the lines go upwards (or right to left) instead.</summary>
    public bool WrapReverse
    {
        get => GetValue(WrapReverseProperty);
        set => SetValue(WrapReverseProperty, value);
    }

    /// <summary>Which way each line is packed.</summary>
    public PackDirection PackDirection
    {
        get => GetValue(PackDirectionProperty);
        set => SetValue(PackDirectionProperty, value);
    }

    /// <summary>What decides whether a child fits on a line; Avalonia gives one size, so both policies are the same.</summary>
    public WrapPolicy WrapPolicy
    {
        get => GetValue(WrapPolicyProperty);
        set => SetValue(WrapPolicyProperty, value);
    }

    private bool Horizontal => Orientation == Orientation.Horizontal;

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.WrapBoxAutomationPeer(this);

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        var visible = Children.Where(c => c.IsVisible).ToList();
        foreach (var c in visible)
        {
            c.Measure(Size.Infinity);
        }

        if (visible.Count <= 1)
        {
            if (visible.Count == 1)
            {
                visible[0].Measure(availableSize);
                return visible[0].DesiredSize;
            }

            return default;
        }

        // adw_wrap_layout_measure: the natural length is the children on one line, or the natural line length; the other
        // size is the lines' for the length available.
        var along = 0.0;
        var min = 0.0;
        foreach (var c in visible)
        {
            along += Along(c.DesiredSize) + ChildSpacing;
            min = Math.Max(min, Along(c.DesiredSize));
        }

        along -= ChildSpacing;
        if (NaturalLineLength >= 0)
        {
            along = Math.Max(min, NaturalLineLength);
        }

        var available = Horizontal ? availableSize.Width : availableSize.Height;
        var length = double.IsInfinity(available) ? (NaturalLineLength >= 0 ? NaturalLineLength : -1) : Math.Min(available, along);
        var lines = ComputeLines(visible, length);
        var across = LineHomogeneous ? lines.Max(l => l.Size) * lines.Count : lines.Sum(l => l.Size);
        across += LineSpacing * (lines.Count - 1);
        return Horizontal ? new Size(along, across) : new Size(across, along);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var visible = Children.Where(c => c.IsVisible).ToList();
        if (visible.Count == 0)
        {
            return finalSize;
        }

        if (visible.Count == 1)
        {
            visible[0].Arrange(new Rect(finalSize));
            return finalSize;
        }

        // adw_wrap_layout_allocate.
        var length = Horizontal ? finalSize.Width : finalSize.Height;
        var other = Horizontal ? finalSize.Height : finalSize.Width;
        var lines = ComputeLines(visible, length);
        var lineSizes = lines.Select(l => (Min: l.Size, Nat: l.Size, Expand: l.Expand)).ToList();
        var allocated = LineHomogeneous
            ? Enumerable.Repeat(Math.Floor((other - (LineSpacing * (lines.Count - 1))) / lines.Count), lines.Count).ToList()
            : BoxAllocate(lineSizes, other, LineSpacing, JustifyMode.None).Select(a => a.Allocated).ToList();
        var reverse = WrapReverse;
        var pos = reverse ? other + LineSpacing : 0;
        for (var i = 0; i < lines.Count; i++)
        {
            if (reverse)
            {
                pos -= allocated[i] + LineSpacing;
            }

            AllocateLine(lines[i], length, allocated[i], pos, i == lines.Count - 1);
            if (!reverse)
            {
                pos += allocated[i] + LineSpacing;
            }
        }

        return finalSize;
    }

    private double Along(Size s) => Horizontal ? s.Width : s.Height;

    private double Across(Size s) => Horizontal ? s.Height : s.Width;

    // compute_sizes: the children in lines, each line as tall as its tallest child for the width it gets.
    private List<Line> ComputeLines(List<Control> children, double length)
    {
        var lines = new List<Line>();
        var start = 0;
        while (start < children.Count)
        {
            var count = CountLineChildren(children, start, length);
            if (count == 0)
            {
                count = 1;
            }

            var line = new Line(children.GetRange(start, count));
            var sizes = line.Children.Select(c => (Min: Along(c.DesiredSize), Nat: Along(c.DesiredSize), Expand: AdwStack.GetExpand(c))).ToList();
            if (length >= 0)
            {
                var justify = start + count >= children.Count && !JustifyLastLine ? JustifyMode.None : Justify;
                if (CountLineChildren(children, start, length) == 0)
                {
                    line.Sizes = [(Math.Max(length, sizes[0].Min), Math.Max(length, sizes[0].Min))];
                }
                else
                {
                    line.Sizes = BoxAllocate(sizes, length, ChildSpacing, justify);
                }
            }
            else
            {
                line.Sizes = sizes.Select(s => (s.Nat, s.Nat)).ToList();
            }

            for (var j = 0; j < count; j++)
            {
                var c = line.Children[j];
                c.Measure(Horizontal ? new Size(line.Sizes[j].Allocated, double.PositiveInfinity) : new Size(double.PositiveInfinity, line.Sizes[j].Allocated));
                line.Size = Math.Max(line.Size, Across(c.DesiredSize));
            }

            line.Expand = false;
            lines.Add(line);
            start += count;
        }

        return lines;
    }

    // count_line_children.
    private int CountLineChildren(List<Control> children, int start, double length)
    {
        if (length < 0)
        {
            return children.Count - start;
        }

        var remaining = length + ChildSpacing;
        var n = 0;
        while (start + n < children.Count)
        {
            var delta = Along(children[start + n].DesiredSize) + ChildSpacing;
            if (remaining < delta - 1e-9)
            {
                break;
            }

            remaining -= delta;
            n++;
        }

        return n;
    }

    // box_allocate: up to the minimums, then the naturals, then the extra to the expanding children (or all, justified).
    private static List<(double Available, double Allocated)> BoxAllocate(List<(double Min, double Nat, bool Expand)> children, double forSize, double spacing, JustifyMode justify)
    {
        var n = children.Count;
        var nExpand = children.Count(c => c.Expand);
        var extra = Math.Max(0, forSize - ((n - 1) * spacing) - children.Sum(c => c.Min));
        var sizes = children.Select(c => c.Min).ToArray();

        // gtk_distribute_natural_allocation: the smallest gaps to natural first.
        var gaps = children.Select((c, i) => (Gap: c.Nat - c.Min, Index: i)).OrderBy(g => g.Gap).ToList();
        for (var i = 0; i < gaps.Count && extra > 0; i++)
        {
            var share = Math.Min(gaps[i].Gap, Math.Ceiling(extra / (gaps.Count - i)));
            sizes[gaps[i].Index] += share;
            extra -= share;
        }

        double given = 0, extraWidgets = 0;
        if (nExpand > 0)
        {
            given = Math.Floor(extra / nExpand);
            extraWidgets = extra % nExpand;
        }
        else if (justify != JustifyMode.None)
        {
            given = Math.Floor(extra / n);
            extraWidgets = extra % n;
        }

        var result = new List<(double Available, double Allocated)>(n);
        for (var i = 0; i < n; i++)
        {
            var available = sizes[i];
            if (children[i].Expand || (nExpand == 0 && justify != JustifyMode.None))
            {
                available += given;
                if (extraWidgets > 0)
                {
                    available++;
                    extraWidgets--;
                }
            }

            var allocated = nExpand == 0 && n > 1 && justify == JustifyMode.Spread ? sizes[i] : available;
            result.Add((available, allocated));
        }

        return result;
    }

    // allocate_line; Avalonia mirrors right to left itself.
    private void AllocateLine(Line line, double length, double lineSize, double lineOffset, bool lastLine)
    {
        var spacing = ChildSpacing;
        var n = line.Children.Count;
        var justifyLine = Justify != JustifyMode.None && (!lastLine || JustifyLastLine);
        var reverseLine = PackDirection == PackDirection.EndToStart;
        double offset = 0;
        double allocatedLength = 0;
        if (!justifyLine || reverseLine)
        {
            allocatedLength = (spacing * (n - 1)) + line.Sizes.Sum(s => s.Allocated);
            if (!justifyLine)
            {
                offset += Math.Round((length - allocatedLength) * Align, MidpointRounding.AwayFromZero);
            }
        }

        if (reverseLine)
        {
            offset += allocatedLength + spacing;
        }

        for (var i = 0; i < n; i++)
        {
            var (available, allocated) = line.Sizes[i];
            var delta = available - allocated;
            if (reverseLine)
            {
                offset -= available + spacing;
            }

            var along = offset;
            if (delta > 0)
            {
                var t = n > 1 ? i / (double)(n - 1) : 0;
                along += Math.Round(delta * (reverseLine ? 1 - t : t), MidpointRounding.AwayFromZero);
            }

            var rect = Horizontal ? new Rect(along, lineOffset, allocated, lineSize) : new Rect(lineOffset, along, lineSize, allocated);
            line.Children[i].Arrange(rect);
            if (!reverseLine)
            {
                offset += available + spacing;
            }
        }
    }

    private sealed class Line(List<Control> children)
    {
        public List<Control> Children { get; } = children;

        public List<(double Available, double Allocated)> Sizes { get; set; } = [];

        public double Size { get; set; }

        public bool Expand { get; set; }
    }
}
