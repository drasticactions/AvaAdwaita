using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;

namespace AvaAdwaita.Presenters;

/// <summary>
/// A data template for text content: it shows a string, or a simple value such as an enum or a number, as an
/// <see cref="AdwLabel"/> <c>label</c> node, as a GTK button's child label. Add it to a content presenter's
/// <c>DataTemplates</c>.
/// </summary>
public sealed class LabelTemplate : IRecyclingDataTemplate
{
    /// <summary>The label's node name; empty for a label that is no node of its own, as the text of a <c>label</c> control.</summary>
    public string Node { get; set; } = "label";

    /// <summary>How the text sits in the label: GtkLabel's xalign, which flips in right-to-left text.</summary>
    public Avalonia.Media.TextAlignment TextAlignment { get; set; } = Avalonia.Media.TextAlignment.Start;

    /// <summary>Extra classes for the label node, separated by spaces.</summary>
    public string? Classes { get; set; }

    /// <summary>Treat <c>_</c> as an access key marker.</summary>
    public bool RecognizesAccessKey { get; set; }

    /// <inheritdoc/>
    public bool Match(object? data) => data is System.IConvertible;

    /// <inheritdoc/>
    public Control? Build(object? param) => Build(param, null);

    /// <inheritdoc/>
    public Control? Build(object? data, Control? existing)
    {
        if (existing is AdwLabel reused)
        {
            reused.Text = data?.ToString();
            return reused;
        }

        var label = new AdwLabel { TextAlignment = TextAlignment, RecognizesAccessKey = RecognizesAccessKey };
        if (!string.IsNullOrEmpty(Node))
        {
            Nodes.Css.SetNode(label, Node);
        }

        if (Classes is not null && !string.IsNullOrEmpty(Node))
        {
            Nodes.Css.SetClasses(label, Classes);
        }

        // The presenter sets the label's data context to the content.
        label.Text = data?.ToString();
        label.DataContextChanged += (_, _) => label.Text = label.DataContext?.ToString();
        return label;
    }
}
