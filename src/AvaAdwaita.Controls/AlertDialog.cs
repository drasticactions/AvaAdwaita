using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using AvaAdwaita.Chrome;
using AvaAdwaita.Controls.Primitives;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls;

/// <summary>
/// A dialog that asks the user to choose (AdwAlertDialog): a heading, a body, an optional extra child, and a button per
/// response. <see cref="ChooseAsync"/> shows it and returns the response chosen; Escape chooses
/// <see cref="CloseResponse"/> and Enter <see cref="DefaultResponse"/>.
/// </summary>
public class AlertDialog : Dialog
{
    /// <summary>Defines <see cref="Heading"/>.</summary>
    public static readonly StyledProperty<string?> HeadingProperty =
        AvaloniaProperty.Register<AlertDialog, string?>(nameof(Heading));

    /// <summary>Defines <see cref="Body"/>.</summary>
    public static readonly StyledProperty<string?> BodyProperty =
        AvaloniaProperty.Register<AlertDialog, string?>(nameof(Body));

    /// <summary>Defines <see cref="ExtraChild"/>.</summary>
    public static readonly StyledProperty<Control?> ExtraChildProperty =
        AvaloniaProperty.Register<AlertDialog, Control?>(nameof(ExtraChild));

    /// <summary>Defines <see cref="DefaultResponse"/>.</summary>
    public static readonly StyledProperty<string?> DefaultResponseProperty =
        AvaloniaProperty.Register<AlertDialog, string?>(nameof(DefaultResponse));

    /// <summary>Defines <see cref="CloseResponse"/>.</summary>
    public static readonly StyledProperty<string> CloseResponseProperty =
        AvaloniaProperty.Register<AlertDialog, string>(nameof(CloseResponse), "close");

    /// <summary>Defines <see cref="PreferWideLayout"/>.</summary>
    public static readonly StyledProperty<bool> PreferWideLayoutProperty =
        AvaloniaProperty.Register<AlertDialog, bool>(nameof(PreferWideLayout));

    private readonly AlertContents _contents = new();
    private readonly AlertHeading _heading = new();
    private readonly AdwLabel _body = new() { TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap };
    private readonly AdwBox _childBin = new();
    private readonly AdwBox _messageArea = new();
    private readonly AlertResponses _responses = new();
    private TaskCompletionSource<string>? _choice;
    private string? _chosen;

    static AlertDialog()
    {
        CssNodeProviders.Register<AlertDialog>(_ => ["alert"]);
        PresentationModeProperty.OverrideDefaultValue<AlertDialog>(DialogPresentationMode.Floating);
    }

    /// <summary>Creates an alert dialog.</summary>
    public AlertDialog()
    {
        Css.SetNode(_body, "label");
        Css.SetClasses(_body, "body");
        Css.SetNode(_childBin, "widget");
        Css.SetClasses(_childBin, "child");
        Css.SetNode(_messageArea, "box");
        _childBin.IsVisible = false;
        _heading.IsVisible = false;
        _body.IsVisible = false;
        _messageArea.Child = new AdwStack { Orientation = Orientation.Vertical, Children = { _heading, _body, _childBin } };
        var box = new AdwBox { Child = new AdwStack { Orientation = Orientation.Vertical, Children =
        {
            new ScrollViewer
            {
                Classes = { "body-scrolled-window", "undershoot-bottom" },
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = _messageArea,
            },
            _responses,
        } } };
        Css.SetNode(box, "box");
        Css.SetClasses(box, "vertical");
        _contents.Child = new HeaderBarHandle { Child = box };
        _contents.Heading = _heading;
        _contents.Responses = _responses;
        Content = _contents;
        Responses.CollectionChanged += OnResponsesChanged;
        UpdateMessageArea();
    }

    /// <summary>Raised when a response is chosen, with its id.</summary>
    public event EventHandler<string>? Response;

    /// <summary>The heading.</summary>
    public string? Heading
    {
        get => GetValue(HeadingProperty);
        set => SetValue(HeadingProperty, value);
    }

    /// <summary>The body, under the heading.</summary>
    public string? Body
    {
        get => GetValue(BodyProperty);
        set => SetValue(BodyProperty, value);
    }

    /// <summary>A widget under the body, as an entry or a list.</summary>
    public Control? ExtraChild
    {
        get => GetValue(ExtraChildProperty);
        set => SetValue(ExtraChildProperty, value);
    }

    /// <summary>The response Enter chooses.</summary>
    public string? DefaultResponse
    {
        get => GetValue(DefaultResponseProperty);
        set => SetValue(DefaultResponseProperty, value);
    }

    /// <summary>The response Escape and closing the dialog choose.</summary>
    public string CloseResponse
    {
        get => GetValue(CloseResponseProperty);
        set => SetValue(CloseResponseProperty, value);
    }

    /// <summary>Lets responses stay side by side in a wider dialog rather than stack.</summary>
    public bool PreferWideLayout
    {
        get => GetValue(PreferWideLayoutProperty);
        set => SetValue(PreferWideLayoutProperty, value);
    }

    /// <summary>The responses, a button each, in order from the start (or the bottom, stacked).</summary>
    public AvaloniaList<AlertDialogResponse> Responses { get; } = new();

    /// <summary>Shows the dialog and returns the id of the response chosen, or <see cref="CloseResponse"/>.</summary>
    public Task<string> ChooseAsync(Visual parent)
    {
        _choice = new TaskCompletionSource<string>();
        _chosen = null;
        var task = _choice.Task;
        Present(parent);
        return task;
    }

    /// <summary>Adds a response.</summary>
    public void AddResponse(string id, string label, ResponseAppearance appearance = ResponseAppearance.Default) =>
        Responses.Add(new AlertDialogResponse(id, label, appearance));

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == HeadingProperty || change.Property == BodyProperty || change.Property == ExtraChildProperty)
        {
            UpdateMessageArea();
        }
        else if (change.Property == PreferWideLayoutProperty)
        {
            _contents.PreferWideLayout = PreferWideLayout;
            _contents.Relayout();
        }
        else if (change.Property == DefaultResponseProperty)
        {
            RebuildResponses();
        }
        else if (change.Property == BoundsProperty)
        {
            var isShort = Classes.Contains("short") || (Bounds.Height is > 0 and <= LandscapeHeight);
            if (isShort != _contents.IsShort)
            {
                _heading.IsShort = _responses.IsShort = _contents.IsShort = isShort;
                _contents.Relayout();
            }
        }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.AlertDialogAutomationPeer(this);

    /// <summary>Chooses <paramref name="id"/>: raises <see cref="Response"/> and closes the dialog.</summary>
    internal void Choose(string id)
    {
        if (_chosen is not null)
        {
            return;
        }

        _chosen = id;
        Response?.Invoke(this, id);
        ForceClose();
    }

    /// <inheritdoc/>
    protected override void OnClosed()
    {
        // Closing without a response is the close response.
        var id = _chosen ?? CloseResponse;
        if (_chosen is null)
        {
            _chosen = id;
            Response?.Invoke(this, id);
        }

        _choice?.TrySetResult(id);
        _choice = null;
        _chosen = null;
    }

    private void UpdateMessageArea()
    {
        _heading.Text = Heading;
        _heading.IsVisible = !string.IsNullOrEmpty(Heading);
        _body.Text = Body;
        _body.IsVisible = !string.IsNullOrEmpty(Body);
        _childBin.Child = ExtraChild;
        _childBin.IsVisible = ExtraChild is not null;
        Css.SetClasses(_messageArea, "vertical message-area"
            + (_heading.IsVisible ? " has-heading" : string.Empty)
            + (_body.IsVisible ? " has-body" : string.Empty));
        _contents.Relayout();
    }

    private void OnResponsesChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildResponses();

    private void RebuildResponses()
    {
        _responses.Children.Clear();
        foreach (var r in Responses)
        {
            var button = new Button
            {
                Content = r.Label,
                IsDefault = r.Id == DefaultResponse,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
            };
            button.Bind(IsEnabledProperty, r.GetObservable(AlertDialogResponse.IsEnabledProperty));
            button.Classes.Set("suggested-action", r.Appearance == ResponseAppearance.Suggested);
            button.Classes.Set("destructive-action", r.Appearance == ResponseAppearance.Destructive);
            var id = r.Id;
            button.Click += (_, _) => Choose(id);
            _responses.Children.Add(button);
        }

        _contents.Relayout();
    }
}
