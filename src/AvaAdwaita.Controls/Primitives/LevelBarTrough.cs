using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>
/// A level bar's trough: its block nodes, laid out as gtklevelbar.c's allocate_trough_continuous and
/// allocate_trough_discrete. A continuous bar draws its empty block under the filled one.
/// </summary>
public class LevelBarTrough : Panel
{
    private LevelBar? _bar;

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _bar = TemplatedParent as LevelBar;
        if (_bar is not null)
        {
            _bar.PropertyChanged += OnBarPropertyChanged;
            _bar.OffsetsChanged += OnOffsetsChanged;
        }

        Update();
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_bar is not null)
        {
            _bar.PropertyChanged -= OnBarPropertyChanged;
            _bar.OffsetsChanged -= OnOffsetsChanged;
        }

        _bar = null;
    }

    private void OnOffsetsChanged(object? sender, EventArgs e) => Update();

    private void OnBarPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == RangeBase.ValueProperty || e.Property == RangeBase.MinimumProperty || e.Property == RangeBase.MaximumProperty
            || e.Property == LevelBar.ModeProperty || e.Property == LevelBar.OrientationProperty || e.Property == LevelBar.IsInvertedProperty
            || e.Property == FlowDirectionProperty)
        {
            Update();
        }
    }

    private bool Continuous => _bar?.Mode != LevelBarMode.Discrete;

    private int BlockCount => _bar is null ? 0 : Continuous ? 1 : Math.Max(1, (int)(Math.Round(_bar.Maximum) - Math.Round(_bar.Minimum)));

    // Avalonia mirrors a right-to-left bar itself, so only the property inverts it here.
    private bool Inverted => _bar?.IsInverted == true;

    private void Update()
    {
        if (_bar is null)
        {
            return;
        }

        var nodes = Continuous ? 2 : BlockCount;
        while (Children.Count > nodes)
        {
            Children.RemoveAt(Children.Count - 1);
        }

        while (Children.Count < nodes)
        {
            var block = new AdwBox();
            Css.SetNode(block, "block");
            Children.Add(block);
        }

        // update_level_style_classes: the filled blocks, then the empty ones.
        var valueClass = _bar.ValueClass();
        var filled = Continuous ? 1 : Math.Min(nodes, (int)Math.Round(_bar.Value) - (int)Math.Round(_bar.Minimum));
        for (var i = 0; i < nodes; i++)
        {
            var block = Children[Inverted ? nodes - 1 - i : i];
            Css.SetClasses(block, i < filled ? (valueClass is null ? "filled" : "filled " + valueClass) : "empty");
        }

        if (Continuous)
        {
            // The empty block is drawn first and the filled one over it; a bar at zero draws no filled block.
            Children[Inverted ? 1 : 0].ZIndex = 1;
            Children[Inverted ? 1 : 0].Opacity = _bar.Value == 0 ? 0 : 1;
            Children[Inverted ? 0 : 1].ZIndex = 0;
        }

        InvalidateMeasure();
    }

    private Size MinBlock()
    {
        var size = default(Size);
        foreach (var c in Children)
        {
            size = new Size(Math.Max(size.Width, c.DesiredSize.Width), Math.Max(size.Height, c.DesiredSize.Height));
        }

        return size;
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var c in Children)
        {
            c.Measure(Size.Infinity);
        }

        var block = MinBlock();
        var horizontal = _bar?.Orientation != Orientation.Vertical;
        return horizontal ? new Size(BlockCount * block.Width, block.Height) : new Size(block.Width, BlockCount * block.Height);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (_bar is null || Children.Count == 0)
        {
            return finalSize;
        }

        var horizontal = _bar.Orientation != Orientation.Vertical;
        var (width, height) = ((int)finalSize.Width, (int)finalSize.Height);
        if (Continuous)
        {
            var filledBlock = Children[Inverted ? 1 : 0];
            Children[Inverted ? 0 : 1].Arrange(new Rect(0, 0, width, height));
            var range = _bar.Maximum - _bar.Minimum;
            var fill = range > 0 ? (_bar.Value - _bar.Minimum) / range : 0;
            var min = horizontal ? filledBlock.DesiredSize.Width : filledBlock.DesiredSize.Height;
            if (horizontal)
            {
                var w = Math.Max(Math.Floor(width * fill), min);
                filledBlock.Arrange(new Rect(Inverted ? width - w : 0, 0, w, height));
            }
            else
            {
                var h = Math.Max(Math.Floor(height * fill), min);
                filledBlock.Arrange(new Rect(0, Inverted ? height - h : 0, width, h));
            }

            return finalSize;
        }

        var block = MinBlock();
        var count = BlockCount;
        double blockWidth, blockHeight, extra;
        if (horizontal)
        {
            blockWidth = Math.Max(block.Width, Math.Floor((double)width / count));
            blockHeight = height;
            extra = width - (blockWidth * count);
            blockWidth += extra > 0 ? 1 : 0;
        }
        else
        {
            blockWidth = width;
            blockHeight = Math.Max(block.Height, Math.Floor((double)height / count));
            extra = height - (blockHeight * count);
            blockHeight += extra > 0 ? 1 : 0;
        }

        var x = 0.0;
        var y = 0.0;
        for (var i = 0; i < count; i++)
        {
            if (extra > 0 && i == extra)
            {
                if (horizontal)
                {
                    blockWidth--;
                }
                else
                {
                    blockHeight--;
                }
            }

            Children[i].Arrange(new Rect(x, y, blockWidth, blockHeight));
            if (horizontal)
            {
                x += blockWidth;
            }
            else
            {
                y += blockHeight;
            }
        }

        return finalSize;
    }
}
