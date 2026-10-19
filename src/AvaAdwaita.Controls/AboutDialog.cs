using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaAdwaita.Controls.Primitives;
using AvaAdwaita.Controls.Resources;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls;

/// <summary>
/// A dialog about an application (AdwAboutDialog): its icon, name, developer and version, then rows for what's new,
/// details and website, support and troubleshooting, and credits, legal and acknowledgements, each opening a page of its
/// own. Clicking the version copies it. Only what is set shows.
/// </summary>
public partial class AboutDialog : Dialog
{
    /// <summary>Defines <see cref="ApplicationIcon"/>.</summary>
    public static readonly StyledProperty<string?> ApplicationIconProperty =
        AvaloniaProperty.Register<AboutDialog, string?>(nameof(ApplicationIcon));

    /// <summary>Defines <see cref="ApplicationName"/>.</summary>
    public static readonly StyledProperty<string?> ApplicationNameProperty =
        AvaloniaProperty.Register<AboutDialog, string?>(nameof(ApplicationName));

    /// <summary>Defines <see cref="DeveloperName"/>.</summary>
    public static readonly StyledProperty<string?> DeveloperNameProperty =
        AvaloniaProperty.Register<AboutDialog, string?>(nameof(DeveloperName));

    /// <summary>Defines <see cref="Version"/>.</summary>
    public static readonly StyledProperty<string?> VersionProperty =
        AvaloniaProperty.Register<AboutDialog, string?>(nameof(Version));

    /// <summary>Defines <see cref="ReleaseNotesVersion"/>.</summary>
    public static readonly StyledProperty<string?> ReleaseNotesVersionProperty =
        AvaloniaProperty.Register<AboutDialog, string?>(nameof(ReleaseNotesVersion));

    /// <summary>Defines <see cref="ReleaseNotes"/>.</summary>
    public static readonly StyledProperty<string?> ReleaseNotesProperty =
        AvaloniaProperty.Register<AboutDialog, string?>(nameof(ReleaseNotes));

    /// <summary>Defines <see cref="Comments"/>.</summary>
    public static readonly StyledProperty<string?> CommentsProperty =
        AvaloniaProperty.Register<AboutDialog, string?>(nameof(Comments));

    /// <summary>Defines <see cref="Website"/>.</summary>
    public static readonly StyledProperty<string?> WebsiteProperty =
        AvaloniaProperty.Register<AboutDialog, string?>(nameof(Website));

    /// <summary>Defines <see cref="SupportUrl"/>.</summary>
    public static readonly StyledProperty<string?> SupportUrlProperty =
        AvaloniaProperty.Register<AboutDialog, string?>(nameof(SupportUrl));

    /// <summary>Defines <see cref="IssueUrl"/>.</summary>
    public static readonly StyledProperty<string?> IssueUrlProperty =
        AvaloniaProperty.Register<AboutDialog, string?>(nameof(IssueUrl));

    /// <summary>Defines <see cref="DebugInfo"/>.</summary>
    public static readonly StyledProperty<string?> DebugInfoProperty =
        AvaloniaProperty.Register<AboutDialog, string?>(nameof(DebugInfo));

    /// <summary>Defines <see cref="DebugInfoFilename"/>.</summary>
    public static readonly StyledProperty<string?> DebugInfoFilenameProperty =
        AvaloniaProperty.Register<AboutDialog, string?>(nameof(DebugInfoFilename));

    /// <summary>Defines <see cref="TranslatorCredits"/>.</summary>
    public static readonly StyledProperty<string?> TranslatorCreditsProperty =
        AvaloniaProperty.Register<AboutDialog, string?>(nameof(TranslatorCredits));

    /// <summary>Defines <see cref="Copyright"/>.</summary>
    public static readonly StyledProperty<string?> CopyrightProperty =
        AvaloniaProperty.Register<AboutDialog, string?>(nameof(Copyright));

    /// <summary>Defines <see cref="LicenseType"/>.</summary>
    public static readonly StyledProperty<License> LicenseTypeProperty =
        AvaloniaProperty.Register<AboutDialog, License>(nameof(LicenseType));

    /// <summary>Defines <see cref="License"/>.</summary>
    public static readonly StyledProperty<string?> LicenseProperty =
        AvaloniaProperty.Register<AboutDialog, string?>(nameof(License));

    private static readonly (License Type, string Url)[] s_licenseUrls =
    [
        (Controls.License.Gpl20, "https://www.gnu.org/licenses/old-licenses/gpl-2.0.html"),
        (Controls.License.Gpl30, "https://www.gnu.org/licenses/gpl-3.0.html"),
        (Controls.License.Lgpl21, "https://www.gnu.org/licenses/old-licenses/lgpl-2.1.html"),
        (Controls.License.Lgpl30, "https://www.gnu.org/licenses/lgpl-3.0.html"),
        (Controls.License.Bsd, "https://opensource.org/licenses/bsd-license.php"),
        (Controls.License.MitX11, "https://opensource.org/licenses/mit-license.php"),
        (Controls.License.Artistic, "https://opensource.org/licenses/artistic-license-2.0.php"),
        (Controls.License.Gpl20Only, "https://www.gnu.org/licenses/old-licenses/gpl-2.0.html"),
        (Controls.License.Gpl30Only, "https://www.gnu.org/licenses/gpl-3.0.html"),
        (Controls.License.Lgpl21Only, "https://www.gnu.org/licenses/old-licenses/lgpl-2.1.html"),
        (Controls.License.Lgpl30Only, "https://www.gnu.org/licenses/lgpl-3.0.html"),
        (Controls.License.Agpl30, "https://www.gnu.org/licenses/agpl-3.0.html"),
        (Controls.License.Agpl30Only, "https://www.gnu.org/licenses/agpl-3.0.html"),
        (Controls.License.Bsd3, "https://opensource.org/licenses/BSD-3-Clause"),
        (Controls.License.Apache20, "https://opensource.org/licenses/Apache-2.0"),
        (Controls.License.Mpl20, "https://opensource.org/licenses/MPL-2.0"),
        (Controls.License.ZeroBsd, "https://opensource.org/license/0bsd"),
    ];

    private readonly NavigationView _navigation = new();
    private readonly NavigationPage _mainPage = new();
    private readonly Revealer _titleRevealer = new() { Transition = RevealerTransition.SlideUp, VerticalAlignment = VerticalAlignment.Center };
    private readonly WindowTitle _windowTitle = new();
    private readonly ScrollViewer _mainScroll = new() { HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
    private readonly AdwIcon _icon = new() { PixelSize = 128, IsVisible = false };
    private readonly AdwLabel _name = new() { TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, IsVisible = false };
    private readonly AdwLabel _developer = new() { TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, IsVisible = false };
    private readonly Button _version = new() { HorizontalAlignment = HorizontalAlignment.Center, IsVisible = false };
    private readonly PreferencesGroup _detailsGroup = new() { IsVisible = false };
    private readonly ActionRow _whatsNewRow;
    private readonly ActionRow _detailsRow;
    private readonly LinkRow _websiteRow = new() { UseUnderline = true, IsVisible = false };
    private readonly PreferencesGroup _supportGroup = new() { IsVisible = false };
    private readonly LinkRow _supportRow = new() { UseUnderline = true, IsVisible = false };
    private readonly LinkRow _issueRow = new() { UseUnderline = true, IsVisible = false };
    private readonly ActionRow _troubleshootingRow;
    private readonly PreferencesGroup _creditsLegalGroup = new() { IsVisible = false };
    private readonly ActionRow _creditsRow;
    private readonly ActionRow _legalRow;
    private readonly ActionRow _acknowledgementsRow;
    private readonly PreferencesGroup _otherAppsGroup = new() { IsVisible = false };
    private readonly SelectableTextBlock _releaseNotesText = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(12, 18) };
    private readonly MarkupLabel _comments = new() { IsVisible = false };
    private readonly PreferencesGroup _linksGroup = new() { IsVisible = false };
    private readonly LinkRow _detailsWebsiteRow = new() { UseUnderline = true, IsVisible = false };
    private readonly SelectableTextBlock _debugInfoText = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(12, 18) };
    private readonly AdwStack _creditsBox = new() { Orientation = Orientation.Vertical };
    private readonly AdwStack _legalBox = new() { Orientation = Orientation.Vertical };
    private readonly AdwStack _acknowledgementsBox = new() { Orientation = Orientation.Vertical };
    private readonly NavigationPage _debugInfoPage;
    private readonly List<(string Name, IReadOnlyList<string> People)> _creditSections = [];
    private readonly List<(string Name, IReadOnlyList<string> People)> _acknowledgementSections = [];
    private readonly List<(string Title, string? Copyright, License LicenseType, string? License)> _legalSections = [];
    private bool _hasCustomLinks;

    static AboutDialog()
    {
        CssNodeProviders.Register<AboutDialog>(_ => ["about"]);
        ContentWidthProperty.OverrideDefaultValue<AboutDialog>(360);
    }

    /// <summary>Creates an about dialog.</summary>
    public AboutDialog()
    {
        MinWidth = 360;
        MinHeight = 200;
        this[!TitleProperty] = this.GetResourceObservable("AdwStringAbout").ToBinding();
        _mainPage.Bind(NavigationPage.TitleProperty, this.GetObservable(TitleProperty));
        _windowTitle.Bind(WindowTitle.TitleProperty, this.GetObservable(ApplicationNameProperty));
        _name.Bind(TextBlock.TextProperty, this.GetObservable(ApplicationNameProperty));
        _developer.Bind(TextBlock.TextProperty, this.GetObservable(DeveloperNameProperty));
        _version.Bind(ContentProperty, this.GetObservable(VersionProperty));
        _icon.Bind(AdwIcon.IconNameProperty, this.GetObservable(ApplicationIconProperty));
        _websiteRow.Bind(LinkRow.UriProperty, this.GetObservable(WebsiteProperty));
        _detailsWebsiteRow.Bind(LinkRow.UriProperty, this.GetObservable(WebsiteProperty));
        _supportRow.Bind(LinkRow.UriProperty, this.GetObservable(SupportUrlProperty));
        _issueRow.Bind(LinkRow.UriProperty, this.GetObservable(IssueUrlProperty));
        _comments.Bind(MarkupLabel.MarkupProperty, this.GetObservable(CommentsProperty));
        _debugInfoText.Bind(TextBlock.TextProperty, this.GetObservable(DebugInfoProperty));
        BindTitle(_websiteRow, "AdwStringWebsiteRow");
        BindTitle(_detailsWebsiteRow, "AdwStringWebsiteRow");
        BindTitle(_supportRow, "AdwStringSupportRow");
        BindTitle(_issueRow, "AdwStringIssueRow");
        _whatsNewRow = PushRow("AdwStringWhatsNewRow", "whatsnew");
        _detailsRow = PushRow("AdwStringDetailsRow", "details");
        _troubleshootingRow = PushRow("AdwStringTroubleshootingRow", "troubleshooting");
        _creditsRow = PushRow("AdwStringCreditsRow", "credits");
        _legalRow = PushRow("AdwStringLegalRow", "legal");
        _acknowledgementsRow = PushRow("AdwStringAcknowledgementsRow", "acknowledgements");
        _whatsNewRow.IsVisible = _detailsRow.IsVisible = _troubleshootingRow.IsVisible = false;
        _creditsRow.IsVisible = _legalRow.IsVisible = _acknowledgementsRow.IsVisible = false;
        foreach (var row in new[] { _websiteRow, _detailsWebsiteRow, _supportRow, _issueRow })
        {
            row.LinkActivated += OnRowLinkActivated;
        }

        _comments.LinkActivated += OnLabelLinkActivated;
        _version.Classes.Add("app-version");
        _version.Click += (_, _) => CopyAsync(Version);

        // The main page: the application, then its groups of rows.
        Css.SetNode(_icon, "image");
        Css.SetClasses(_icon, "icon-dropshadow");
        Css.SetNode(_name, "label");
        Css.SetClasses(_name, "title-1");
        Css.SetNode(_developer, "label");
        _detailsGroup.Children.AddRange([_whatsNewRow, _detailsRow, _websiteRow]);
        _supportGroup.Children.AddRange([_supportRow, _issueRow, _troubleshootingRow]);
        _creditsLegalGroup.Children.AddRange([_creditsRow, _legalRow, _acknowledgementsRow]);
        _mainScroll.Classes.Add("main-page");
        _mainScroll.Content = new Clamp
        {
            Child = Box("vertical", _icon, _name, _developer, _version, Box("vertical", _detailsGroup, _supportGroup, _creditsLegalGroup, _otherAppsGroup)),
        };
        _mainScroll.ScrollChanged += (_, _) => UpdateTitleRevealer();
        _titleRevealer.Child = _windowTitle;
        _mainPage.Content = new ToolbarView { TopBars = { new HeaderBar { TitleWidget = _titleRevealer } }, Content = _mainScroll };

        // The pages the rows open.
        Css.SetClasses(_comments, "body");
        _linksGroup.Children.Add(_detailsWebsiteRow);
        var troubleshootingText = new AdwLabel { TextWrapping = TextWrapping.Wrap };
        Css.SetNode(troubleshootingText, "label");
        Css.SetClasses(troubleshootingText, "body");
        troubleshootingText[!TextBlock.TextProperty] = troubleshootingText.GetResourceObservable("AdwStringTroubleshootingText").ToBinding();
        var debugInfoRow = PushRow("AdwStringDebuggingInformationRow", "debuginfo");
        var copy = new Button { HorizontalContentAlignment = HorizontalAlignment.Center };
        copy[!ContentProperty] = copy.GetResourceObservable("AdwStringCopyText").ToBinding();
        copy.Click += (_, _) => CopyAsync(DebugInfo);
        var save = new Button();
        save[!ContentProperty] = save.GetResourceObservable("AdwStringSaveAs").ToBinding();
        save.Click += (_, _) => _ = SaveDebugInfoAsync();
        _debugInfoPage = Subpage("AdwStringDebuggingInformation", "debuginfo", TextView(_debugInfoText), new ActionBar { Start = { copy }, End = { save } });
        _navigation.Pages.AddRange(
        [
            _mainPage,
            Subpage("AdwStringWhatsNew", "whatsnew", TextView(_releaseNotesText)),
            Subpage("AdwStringDetails", "details", Box("vertical", _comments, _linksGroup)),
            Subpage("AdwStringTroubleshooting", "troubleshooting", Box("vertical", troubleshootingText, new PreferencesGroup { Children = { debugInfoRow } })),
            _debugInfoPage,
            Subpage("AdwStringCredits", "credits", Box("vertical", _creditsBox)),
            Subpage("AdwStringLegal", "legal", Box("vertical", _legalBox)),
            Subpage("AdwStringAcknowledgements", "acknowledgements", Box("vertical", _acknowledgementsBox)),
        ]);
        var overlay = new AdwBox { Child = _navigation };
        Css.SetNode(overlay, "toastoverlay");
        Content = overlay;
        Developers.CollectionChanged += (_, _) => UpdateCredits();
        Designers.CollectionChanged += (_, _) => UpdateCredits();
        Artists.CollectionChanged += (_, _) => UpdateCredits();
        Documenters.CollectionChanged += (_, _) => UpdateCredits();
    }

    /// <summary>Raised when a link is activated, with its URI; handle it to keep the dialog from opening the URI.</summary>
    public event EventHandler<LinkActivatedEventArgs>? LinkActivated;

    /// <summary>The application's icon name, shown large at the top.</summary>
    public string? ApplicationIcon
    {
        get => GetValue(ApplicationIconProperty);
        set => SetValue(ApplicationIconProperty, value);
    }

    /// <summary>The application's name.</summary>
    public string? ApplicationName
    {
        get => GetValue(ApplicationNameProperty);
        set => SetValue(ApplicationNameProperty, value);
    }

    /// <summary>The developer's name, under the application's.</summary>
    public string? DeveloperName
    {
        get => GetValue(DeveloperNameProperty);
        set => SetValue(DeveloperNameProperty, value);
    }

    /// <summary>The version, on a button that copies it.</summary>
    public string? Version
    {
        get => GetValue(VersionProperty);
        set => SetValue(VersionProperty, value);
    }

    /// <summary>The version <see cref="ReleaseNotes"/> are for; <see cref="Version"/> when unset.</summary>
    public string? ReleaseNotesVersion
    {
        get => GetValue(ReleaseNotesVersionProperty);
        set => SetValue(ReleaseNotesVersionProperty, value);
    }

    /// <summary>Release notes in AppStream markup (<c>p</c>, <c>ul</c>, <c>ol</c>, <c>li</c>, <c>em</c>, <c>code</c>), for the What's New page.</summary>
    public string? ReleaseNotes
    {
        get => GetValue(ReleaseNotesProperty);
        set => SetValue(ReleaseNotesProperty, value);
    }

    /// <summary>A description in Pango markup, for the Details page.</summary>
    public string? Comments
    {
        get => GetValue(CommentsProperty);
        set => SetValue(CommentsProperty, value);
    }

    /// <summary>The application's website.</summary>
    public string? Website
    {
        get => GetValue(WebsiteProperty);
        set => SetValue(WebsiteProperty, value);
    }

    /// <summary>Where to ask for help.</summary>
    public string? SupportUrl
    {
        get => GetValue(SupportUrlProperty);
        set => SetValue(SupportUrlProperty, value);
    }

    /// <summary>Where to report an issue.</summary>
    public string? IssueUrl
    {
        get => GetValue(IssueUrlProperty);
        set => SetValue(IssueUrlProperty, value);
    }

    /// <summary>Debugging information, for the Troubleshooting page, to copy or save.</summary>
    public string? DebugInfo
    {
        get => GetValue(DebugInfoProperty);
        set => SetValue(DebugInfoProperty, value);
    }

    /// <summary>The file name suggested for saving <see cref="DebugInfo"/>.</summary>
    public string? DebugInfoFilename
    {
        get => GetValue(DebugInfoFilenameProperty);
        set => SetValue(DebugInfoFilenameProperty, value);
    }

    /// <summary>The developers, each "Name", "Name &lt;email&gt;" or "Name https://url".</summary>
    public AvaloniaList<string> Developers { get; } = new();

    /// <summary>The designers, as <see cref="Developers"/>.</summary>
    public AvaloniaList<string> Designers { get; } = new();

    /// <summary>The artists, as <see cref="Developers"/>.</summary>
    public AvaloniaList<string> Artists { get; } = new();

    /// <summary>The documenters, as <see cref="Developers"/>.</summary>
    public AvaloniaList<string> Documenters { get; } = new();

    /// <summary>The translators, a line each, as <see cref="Developers"/>.</summary>
    public string? TranslatorCredits
    {
        get => GetValue(TranslatorCreditsProperty);
        set => SetValue(TranslatorCreditsProperty, value);
    }

    /// <summary>The copyright notice, in Pango markup.</summary>
    public string? Copyright
    {
        get => GetValue(CopyrightProperty);
        set => SetValue(CopyrightProperty, value);
    }

    /// <summary>The license, named and linked; <see cref="Controls.License.Custom"/> shows <see cref="License"/>.</summary>
    public License LicenseType
    {
        get => GetValue(LicenseTypeProperty);
        set => SetValue(LicenseTypeProperty, value);
    }

    /// <summary>The license text in Pango markup, for <see cref="Controls.License.Custom"/>.</summary>
    public string? License
    {
        get => GetValue(LicenseProperty);
        set => SetValue(LicenseProperty, value);
    }

    /// <summary>Adds a link to the Details page.</summary>
    public void AddLink(string title, string url)
    {
        var row = new LinkRow { Title = title, UseUnderline = true, Uri = url };
        row.LinkActivated += OnRowLinkActivated;
        _linksGroup.Children.Add(row);
        _hasCustomLinks = true;
        UpdateDetails();
    }

    /// <summary>Adds a section of people to the Credits page, as <see cref="Developers"/>.</summary>
    public void AddCreditSection(string name, IEnumerable<string> people)
    {
        _creditSections.Add((name, people.ToList()));
        UpdateCredits();
    }

    /// <summary>Adds a section of people to the Acknowledgements page, as <see cref="Developers"/>.</summary>
    public void AddAcknowledgementSection(string? name, IEnumerable<string> people)
    {
        _acknowledgementSections.Add((name ?? string.Empty, people.ToList()));
        UpdateAcknowledgements();
    }

    /// <summary>Adds a section to the Legal page, as for a component the application uses.</summary>
    public void AddLegalSection(string title, string? copyright, License licenseType, string? license = null)
    {
        _legalSections.Add((title, copyright, licenseType, license));
        UpdateLegal();
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.AboutDialogAutomationPeer(this);

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Section titles and license names are the window's language.
        UpdateCredits();
        UpdateAcknowledgements();
        UpdateLegal();
        UpdateReleaseNotes();
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ApplicationIconProperty)
        {
            _icon.IsVisible = !string.IsNullOrEmpty(ApplicationIcon);
        }
        else if (change.Property == ApplicationNameProperty)
        {
            _name.IsVisible = !string.IsNullOrEmpty(ApplicationName);
        }
        else if (change.Property == DeveloperNameProperty)
        {
            _developer.IsVisible = !string.IsNullOrEmpty(DeveloperName);
        }
        else if (change.Property == VersionProperty)
        {
            _version.IsVisible = !string.IsNullOrEmpty(Version);
            UpdateReleaseNotes();
        }
        else if (change.Property == ReleaseNotesProperty || change.Property == ReleaseNotesVersionProperty)
        {
            UpdateReleaseNotes();
        }
        else if (change.Property == CommentsProperty || change.Property == WebsiteProperty)
        {
            UpdateDetails();
        }
        else if (change.Property == SupportUrlProperty || change.Property == IssueUrlProperty || change.Property == DebugInfoProperty)
        {
            UpdateSupport();
        }
        else if (change.Property == TranslatorCreditsProperty)
        {
            UpdateCredits();
        }
        else if (change.Property == CopyrightProperty || change.Property == LicenseTypeProperty || change.Property == LicenseProperty)
        {
            UpdateLegal();
        }
    }

    private string S(string key) => this.TryFindResource(key, out var value) && value is string text ? text : AdwStrings.English(key);

    private static void BindTitle(PreferencesRow row, string key) =>
        row[!PreferencesRow.TitleProperty] = row.GetResourceObservable(key).ToBinding();

    // A row that opens a page, with the go-next arrow.
    private ActionRow PushRow(string key, string tag)
    {
        var arrow = new AdwIcon { IconName = "go-next-symbolic" };
        Css.SetNode(arrow, "image");
        var row = new ActionRow { UseUnderline = true, IsActivatable = true, Suffixes = { arrow } };
        BindTitle(row, key);
        row.Activated += (_, _) => _navigation.PushByTag(tag);
        return row;
    }

    private static AdwBox Box(string classes, params Control[] children)
    {
        var box = new AdwBox { Child = new AdwStack { Orientation = Orientation.Vertical } };
        Css.SetNode(box, "box");
        Css.SetClasses(box, classes);
        ((AdwStack)box.Child).Children.AddRange(children);
        return box;
    }

    // GtkTextView in a clamp: a read-only text with the body style.
    private static AdwBox TextView(SelectableTextBlock text)
    {
        var textNode = new AdwBox { Child = text };
        Css.SetNode(textNode, "text");
        var view = new AdwBox { Child = textNode };
        Css.SetNode(view, "textview");
        Css.SetClasses(view, "body");
        return view;
    }

    // A page a row opens: a header bar over its content, which scrolls in a clamp.
    private static NavigationPage Subpage(string titleKey, string tag, Control content, Control? bottomBar = null)
    {
        var scroll = new ScrollViewer { HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled, Classes = { "subpage" }, Content = new Clamp { Child = content } };
        var view = new ToolbarView { TopBars = { new HeaderBar() }, Content = scroll };
        if (bottomBar is not null)
        {
            view.BottomBars.Add(bottomBar);
        }

        var page = new NavigationPage { NavigationTag = tag, Content = view };
        page[!NavigationPage.TitleProperty] = page.GetResourceObservable(titleKey).ToBinding();
        return page;
    }

    // update_headerbar_idle_cb: the title shows once the application's name has scrolled out of view.
    private void UpdateTitleRevealer()
    {
        var bottom = _name.IsVisible && _name.TranslatePoint(new Point(0, _name.Bounds.Height), _mainScroll) is { } p ? p.Y : 1;
        _titleRevealer.IsRevealed = bottom < 0;
    }

    // update_details: the website goes in the details page when there are comments or links.
    private void UpdateDetails()
    {
        var hasWebsite = !string.IsNullOrEmpty(Website);
        var hasComments = !string.IsNullOrEmpty(Comments);
        var showDetails = hasComments || _hasCustomLinks;
        var showLinks = (hasWebsite && hasComments) || _hasCustomLinks;
        _comments.IsVisible = hasComments;
        _websiteRow.IsVisible = hasWebsite && !showDetails;
        _detailsWebsiteRow.IsVisible = hasWebsite && showDetails;
        _linksGroup.IsVisible = showLinks;
        _detailsRow.IsVisible = hasComments || showLinks;
        _detailsGroup.IsVisible = hasWebsite || hasComments || showLinks || _whatsNewRow.IsVisible;
    }

    private void UpdateSupport()
    {
        var hasSupport = !string.IsNullOrEmpty(SupportUrl);
        var hasIssue = !string.IsNullOrEmpty(IssueUrl);
        var hasDebugInfo = !string.IsNullOrEmpty(DebugInfo);
        _supportRow.IsVisible = hasSupport;
        _issueRow.IsVisible = hasIssue;
        _troubleshootingRow.IsVisible = hasDebugInfo;
        _supportGroup.IsVisible = hasSupport || hasIssue || hasDebugInfo;
    }

    private void UpdateCreditsLegalGroup()
    {
        _creditsRow.IsVisible = _creditsBox.Children.Count > 0;
        _legalRow.IsVisible = _legalBox.Children.Count > 0;
        _acknowledgementsRow.IsVisible = _acknowledgementsBox.Children.Count > 0;
        _creditsLegalGroup.IsVisible = _creditsRow.IsVisible || _legalRow.IsVisible || _acknowledgementsRow.IsVisible;
    }

    private void UpdateCredits()
    {
        _creditsBox.Children.Clear();
        var translators = TranslatorCredits is { Length: > 0 } t && t is not ("translator_credits" or "translator-credits") ? t.Split('\n') : null;
        AddPeople(_creditsBox, S("AdwStringCodeBy"), Developers);
        AddPeople(_creditsBox, S("AdwStringDesignBy"), Designers);
        AddPeople(_creditsBox, S("AdwStringArtworkBy"), Artists);
        AddPeople(_creditsBox, S("AdwStringDocumentationBy"), Documenters);
        AddPeople(_creditsBox, S("AdwStringTranslatedBy"), translators);
        foreach (var (name, people) in _creditSections)
        {
            AddPeople(_creditsBox, name, people);
        }

        UpdateCreditsLegalGroup();
    }

    private void UpdateAcknowledgements()
    {
        _acknowledgementsBox.Children.Clear();
        foreach (var (name, people) in _acknowledgementSections)
        {
            AddPeople(_acknowledgementsBox, name, people);
        }

        UpdateCreditsLegalGroup();
    }

    // add_credits_section: a group of people, a link row for those with an email address or a website.
    private void AddPeople(AdwStack box, string title, IReadOnlyList<string>? people)
    {
        if (people is null || people.Count == 0)
        {
            return;
        }

        var group = new PreferencesGroup { Title = title };
        foreach (var person in people.Where(p => p.Length > 0))
        {
            var (name, link, isEmail) = ParsePerson(person);
            if (link is null)
            {
                group.Children.Add(new ActionRow { Title = name });
            }
            else
            {
                var row = new LinkRow { Title = name, Uri = isEmail ? "mailto:" + link : link };
                row.LinkActivated += OnRowLinkActivated;
                group.Children.Add(row);
            }
        }

        box.Children.Add(group);
    }

    // parse_person: "Name <email>" or "Name https://url".
    private static (string Name, string? Link, bool IsEmail) ParsePerson(string person)
    {
        var q1 = person.IndexOf('<');
        var q2 = q1 >= 0 ? person.IndexOf('>', q1) : -1;
        var r1 = person.IndexOf("http://", StringComparison.Ordinal);
        var https = person.IndexOf("https://", StringComparison.Ordinal);
        if (r1 < 0 || (https >= 0 && https < r1))
        {
            r1 = https;
        }

        var r2 = -1;
        if (r1 >= 0)
        {
            r2 = person.IndexOfAny([' ', '\n', '\t', '>'], r1);
            r2 = r2 < 0 ? person.Length : r2;
        }

        if (r1 >= 0 && r2 >= 0 && (q1 < 0 || q2 < 0 || r1 <= q1 + 1))
        {
            (q1, q2) = (r1, r2);
        }

        if (q1 >= 0 && q2 >= 0)
        {
            var isEmail = person[q1] == '<';
            var link = isEmail ? person.Substring(q1 + 1, q2 - q1 - 1) : person[q1..q2];
            return (person[..q1].Trim(), link, isEmail);
        }

        return (person.Trim(), null, false);
    }

    private string? LicenseText(License type, string? license)
    {
        if (type == Controls.License.Unknown)
        {
            return null;
        }

        if (type == Controls.License.Custom)
        {
            return license;
        }

        var url = s_licenseUrls.First(l => l.Type == type).Url;
        var name = S("AdwStringLicense" + (type == Controls.License.ZeroBsd ? "0Bsd" : type.ToString()));
        return string.Format(S("AdwStringLicensePreamble"), url, name);
    }

    // update_legal: the application's copyright and license, then each section's under its title.
    private void UpdateLegal()
    {
        _legalBox.Children.Clear();
        AppendLegal(_legalSections.Count > 0 ? S("AdwStringThisApplication") : null, Copyright, LicenseType, License, false);
        foreach (var (title, copyright, type, license) in _legalSections)
        {
            AppendLegal(title, copyright, type, license, true);
        }

        UpdateCreditsLegalGroup();
    }

    private void AppendLegal(string? title, string? copyright, License type, string? license, bool forceTitle)
    {
        var text = LicenseText(type, license);
        var hasCopyright = !string.IsNullOrEmpty(copyright);
        var hasLicense = !string.IsNullOrEmpty(text);
        if (!hasCopyright && !hasLicense && !forceTitle)
        {
            return;
        }

        if (!string.IsNullOrEmpty(title))
        {
            var heading = new AdwLabel { Text = title, TextWrapping = TextWrapping.Wrap };
            Css.SetNode(heading, "label");
            Css.SetClasses(heading, "heading");
            _legalBox.Children.Add(heading);
        }

        if (!hasCopyright && !hasLicense)
        {
            return;
        }

        var label = new MarkupLabel { Markup = hasCopyright && hasLicense ? copyright + "\n\n" + text : hasCopyright ? copyright : text };
        Css.SetClasses(label, "body");
        label.LinkActivated += OnLabelLinkActivated;
        _legalBox.Children.Add(label);
    }

    // update_release_notes: the version as a heading, then the notes' paragraphs and lists.
    private void UpdateReleaseNotes()
    {
        _releaseNotesText.Inlines?.Clear();
        if (string.IsNullOrEmpty(ReleaseNotes))
        {
            _whatsNewRow.IsVisible = false;
            UpdateDetails();
            return;
        }

        _releaseNotesText.Inlines ??= [];
        var version = ReleaseNotesVersion is { Length: > 0 } v ? v : Version is { Length: > 0 } ver ? ver : null;
        if (version is not null)
        {
            _releaseNotesText.Inlines.Add(new Run(string.Format(S("AdwStringVersion"), version)) { FontWeight = FontWeight.Bold });
            _releaseNotesText.Inlines.Add(new LineBreak());
        }

        _releaseNotesText.Inlines.AddRange(ReleaseNotesInlines(ReleaseNotes));
        _whatsNewRow.IsVisible = true;
        UpdateDetails();
    }

    // The AppStream markup parser (start_element_handler, end_element_handler, text_handler) as runs.
    private static List<Inline> ReleaseNotesInlines(string markup)
    {
        var inlines = new List<Inline>();
        try
        {
            using var reader = XmlReader.Create(new StringReader(markup), new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment });
            string? list = null;
            var item = 0;
            var lastTrailingSpace = false;
            var paragraphStart = true;
            var element = new Stack<string>();
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        var name = reader.Name;
                        if (name is "ul" or "ol")
                        {
                            list = name;
                            item = 0;
                        }
                        else if (name == "li")
                        {
                            if (item > 0)
                            {
                                inlines.Add(new LineBreak());
                            }

                            inlines.Add(new Run(list == "ol" ? $"{item + 1}. " : "• "));
                            paragraphStart = true;
                        }
                        else if (name == "p")
                        {
                            paragraphStart = true;
                        }

                        if (!reader.IsEmptyElement)
                        {
                            element.Push(name);
                        }

                        break;
                    case XmlNodeType.EndElement:
                        var end = element.Count > 0 ? element.Pop() : reader.Name;
                        if (end is "p" or "ul" or "ol")
                        {
                            inlines.Add(new LineBreak());
                            list = end == "p" ? list : null;
                        }
                        else if (end == "li")
                        {
                            item++;
                        }

                        break;
                    case XmlNodeType.Text:
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                        if (!element.Any(e => e is "p" or "li"))
                        {
                            break;
                        }

                        // Runs of spaces and newlines become one space.
                        var text = SpaceRuns().Replace(reader.Value, " ");
                        if (text.Length == 0)
                        {
                            break;
                        }

                        var leading = text[0] == ' ';
                        var trailing = text[^1] == ' ';
                        text = text.Trim();
                        if (text.Length == 0)
                        {
                            lastTrailingSpace = trailing;
                            break;
                        }

                        if ((leading || lastTrailingSpace) && !paragraphStart)
                        {
                            text = " " + text;
                        }

                        var run = new Run(text);
                        if (element.Peek() == "em")
                        {
                            run.FontStyle = FontStyle.Italic;
                        }
                        else if (element.Peek() == "code")
                        {
                            run.FontFamily = new FontFamily("monospace");
                        }

                        inlines.Add(run);
                        lastTrailingSpace = trailing;
                        paragraphStart = false;
                        break;
                }
            }

            // Without the trailing newline.
            if (inlines.Count > 0 && inlines[^1] is LineBreak)
            {
                inlines.RemoveAt(inlines.Count - 1);
            }
        }
        catch (XmlException e)
        {
            inlines.Clear();
            inlines.Add(new Run(e.Message));
        }

        return inlines;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex SpaceRuns();

    private void OnRowLinkActivated(object? sender, LinkActivatedEventArgs e)
    {
        LinkActivated?.Invoke(this, e);
    }

    private void OnLabelLinkActivated(object? sender, LinkActivatedEventArgs e)
    {
        LinkActivated?.Invoke(this, e);
    }

    // copy_property_cb.
    private async void CopyAsync(string? text)
    {
        if (string.IsNullOrEmpty(text) || TopLevel.GetTopLevel(this)?.Clipboard is not { } clipboard)
        {
            return;
        }

        await clipboard.SetTextAsync(text);
    }

    // save_debug_info_cb: the debugging information to a file the user picks.
    private async Task SaveDebugInfoAsync()
    {
        if (TopLevel.GetTopLevel(this)?.StorageProvider is not { CanSave: true } storage)
        {
            return;
        }

        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions { Title = S("AdwStringSaveDebuggingInformation"), SuggestedFileName = DebugInfoFilename });
        if (file is null)
        {
            return;
        }

        try
        {
            await using var stream = await file.OpenWriteAsync();
            await stream.WriteAsync(Encoding.UTF8.GetBytes(DebugInfo ?? string.Empty));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            var alert = new AlertDialog { Heading = S("AdwStringUnableToSaveDebuggingInformation"), Body = e.Message };
            alert.AddResponse("close", S("AdwStringClose"));
            alert.Present(this);
        }
    }
}
