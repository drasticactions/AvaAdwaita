using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Media;
using AvaAdwaita.Markup;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>
/// A label showing Pango markup (gtk_label_set_markup): bold, italic, underline, strikethrough, monospace, small and
/// big text, and links, which a click activates. Links take their color from a <c>link</c> node under the label, as
/// GTK's do.
/// </summary>
public class MarkupLabel : AdwLabel
{
    /// <summary>Defines <see cref="Markup"/>.</summary>
    public static readonly StyledProperty<string?> MarkupProperty =
        AvaloniaProperty.Register<MarkupLabel, string?>(nameof(Markup));

    private readonly CssNode _linkNode = new("link") { Visible = false };
    private readonly List<(int Start, int Length, string Uri)> _links = [];
    private readonly List<Run> _linkRuns = [];

    /// <summary>Creates a label: a <c>label</c> node.</summary>
    public MarkupLabel()
    {
        Css.SetNode(this, "label");
        TextWrapping = TextWrapping.Wrap;
        _linkNode.StyleChanged += (_, _) => UpdateLinkBrushes();
    }

    /// <summary>Raised when a link is clicked, with its URI; handle it to keep the label from opening the URI.</summary>
    public event EventHandler<LinkActivatedEventArgs>? LinkActivated;

    /// <summary>The markup.</summary>
    public string? Markup
    {
        get => GetValue(MarkupProperty);
        set => SetValue(MarkupProperty, value);
    }

    /// <summary>The links, by their place in the text.</summary>
    internal IReadOnlyList<(int Start, int Length, string Uri)> Links => _links;

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == MarkupProperty || (change.Property == FontSizeProperty && Markup is not null))
        {
            // Small and big text scale the label's size.
            Build();
        }
        else if (change.Property == Css.StyleProperty && Css.GetCssNode(this) is { } node && _linkNode.Parent != node)
        {
            // The link node follows the label's node, which the theme makes when the label attaches.
            _linkNode.Parent?.RemoveChild(_linkNode);
            node.AppendChild(_linkNode);
        }
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _linkNode.Parent?.RemoveChild(_linkNode);
    }

    /// <inheritdoc/>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        Cursor = LinkAt(e.GetPosition(this)) is null ? null : new Cursor(StandardCursorType.Hand);
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (e.InitialPressMouseButton == MouseButton.Left && LinkAt(e.GetPosition(this)) is { } uri)
        {
            e.Handled = true;
            Activate(uri);
        }
    }

    /// <summary>Activates a link: raises <see cref="LinkActivated"/>, then opens the URI unless that was handled.</summary>
    internal void Activate(string uri)
    {
        var args = new LinkActivatedEventArgs(uri);
        LinkActivated?.Invoke(this, args);
        if (!args.Handled && Uri.TryCreate(uri, UriKind.Absolute, out var u))
        {
            _ = TopLevel.GetTopLevel(this)?.Launcher.LaunchUriAsync(u);
        }
    }

    private string? LinkAt(Point point)
    {
        if (_links.Count == 0)
        {
            return null;
        }

        var hit = TextLayout.HitTestPoint(point - new Point(Padding.Left, Padding.Top));
        if (!hit.IsInside)
        {
            return null;
        }

        var index = hit.TextPosition;
        return _links.FirstOrDefault(l => index >= l.Start && index < l.Start + l.Length).Uri;
    }

    private void UpdateLinkBrushes()
    {
        if (_linkNode.Style is not { } s)
        {
            return;
        }

        var brush = CssConvert.Brush(s.Drawn(s.Core.Color));
        foreach (var run in _linkRuns)
        {
            run.Foreground = brush;
        }
    }

    // Pango markup as runs; markup that does not parse shows as it is.
    private void Build()
    {
        Inlines?.Clear();
        _links.Clear();
        _linkRuns.Clear();
        if (string.IsNullOrEmpty(Markup))
        {
            Text = null;
            return;
        }

        var runs = new List<Run>();
        try
        {
            var settings = new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment, DtdProcessing = DtdProcessing.Ignore };
            using var reader = XmlReader.Create(new System.IO.StringReader(Markup), settings);
            var stack = new Stack<(string Name, string? Href)>();
            var position = 0;
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        var name = reader.Name;
                        var href = name == "a" ? reader.GetAttribute("href") : null;
                        if (!reader.IsEmptyElement)
                        {
                            stack.Push((name, href));
                        }

                        break;
                    case XmlNodeType.EndElement:
                        if (stack.Count > 0)
                        {
                            stack.Pop();
                        }

                        break;
                    case XmlNodeType.Text:
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                        var text = reader.Value;
                        var run = new Run(text);
                        string? link = null;
                        foreach (var (tag, uri) in stack)
                        {
                            switch (tag)
                            {
                                case "b":
                                    run.FontWeight = FontWeight.Bold;
                                    break;
                                case "i":
                                    run.FontStyle = FontStyle.Italic;
                                    break;
                                case "u":
                                    run.TextDecorations = Avalonia.Media.TextDecorations.Underline;
                                    break;
                                case "s":
                                    run.TextDecorations = Avalonia.Media.TextDecorations.Strikethrough;
                                    break;
                                case "tt":
                                    run.FontFamily = new FontFamily("monospace");
                                    break;
                                case "small":
                                    run.FontSize = FontSize / 1.2;
                                    break;
                                case "big":
                                    run.FontSize = FontSize * 1.2;
                                    break;
                                case "a":
                                    link ??= uri;
                                    break;
                            }
                        }

                        if (link is not null)
                        {
                            run.TextDecorations = Avalonia.Media.TextDecorations.Underline;
                            _links.Add((position, text.Length, link));
                            _linkRuns.Add(run);
                        }

                        runs.Add(run);
                        position += text.Length;
                        break;
                }
            }
        }
        catch (XmlException)
        {
            Inlines?.Clear();
            _links.Clear();
            _linkRuns.Clear();
            Text = Markup;
            return;
        }

        Inlines ??= [];
        Inlines.AddRange(runs);
        UpdateLinkBrushes();
    }
}
