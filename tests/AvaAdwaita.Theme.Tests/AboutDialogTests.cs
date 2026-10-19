using System.Linq;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaAdwaita.Controls;
using AvaAdwaita.Controls.Primitives;
using AvaAdwaita.Nodes;
using Xunit;
using NavigationPage = AvaAdwaita.Controls.NavigationPage;

namespace AvaAdwaita.Theme.Tests;

/// <summary>AvaAdwaita.Controls' AboutDialog, LinkRow and MarkupLabel: what shows, the pages and the links.</summary>
public class AboutDialogTests
{
    private static (Window, AboutDialog) Show(AboutDialog dialog)
    {
        var opener = new Button { Content = "Open" };
        var w = Shots.Show(opener, 640, 480);
        AdwaitaEnvironment.SuppressTransitions(w);
        dialog.PresentationMode = DialogPresentationMode.Floating;
        dialog.Present(opener);
        Shots.Pump(w);
        return (w, dialog);
    }

    private static PreferencesRow Row(AboutDialog d, string title) =>
        d.GetVisualDescendants().OfType<PreferencesRow>().Single(r => r.Title == title && r.IsEffectivelyVisible);

    private static NavigationView Navigation(AboutDialog d) => d.GetVisualDescendants().OfType<NavigationView>().First();

    [AvaloniaFact]
    public void Main_Page_Shows_Only_What_Is_Set()
    {
        var (w, dialog) = Show(new AboutDialog { ApplicationName = "Example", Version = "1.0" });
        Assert.Equal("About", dialog.Title);
        Assert.Contains("about", Css.GetCssNode(dialog)!.Classes);
        Assert.False(dialog.GetVisualDescendants().OfType<Presenters.AdwIcon>().Single(i => i.PixelSize == 128).IsVisible);
        Assert.Contains(dialog.GetVisualDescendants().OfType<Button>(), b => b.Classes.Contains("app-version") && b.IsEffectivelyVisible);
        Assert.DoesNotContain(dialog.GetVisualDescendants().OfType<PreferencesRow>(), r => r.IsEffectivelyVisible);

        // A website alone is a row on the main page; with comments it moves to the details page.
        dialog.Website = "https://example.org";
        Shots.Pump(w);
        Assert.IsType<LinkRow>(Row(dialog, "_Website"));
        dialog.Comments = "An <b>example</b>.";
        Shots.Pump(w);
        var details = Row(dialog, "_Details");
        Assert.DoesNotContain(dialog.GetVisualDescendants().OfType<LinkRow>(), r => r.IsEffectivelyVisible);
        details.Activate();
        Shots.Pump(w);
        Assert.Equal("details", Navigation(dialog).VisiblePage!.NavigationTag);
        Assert.Equal("https://example.org", Assert.Single(dialog.GetVisualDescendants().OfType<LinkRow>(), r => r.IsEffectivelyVisible).Uri);
        var comments = dialog.GetVisualDescendants().OfType<MarkupLabel>().Single(l => l.IsEffectivelyVisible);
        Assert.Equal(FontWeight.Bold, comments.Inlines!.OfType<Run>().Single(r => r.Text == "example").FontWeight);
        w.Close();
    }

    [AvaloniaFact]
    public void Credits_And_Legal_Pages_Come_From_The_People_And_The_License()
    {
        var (w, dialog) = Show(new AboutDialog
        {
            ApplicationName = "Example",
            Developers = { "Ada Lovelace <ada@example.org>", "Grace Hopper https://example.org/grace", "Linus" },
            Copyright = "© 2026 Example",
            LicenseType = License.MitX11,
        });
        dialog.AddCreditSection("Thanks", ["Alan"]);
        Shots.Pump(w);
        Row(dialog, "_Credits").Activate();
        Shots.Pump(w);
        var page = Navigation(dialog).VisiblePage!;
        var groups = page.GetVisualDescendants().OfType<PreferencesGroup>().ToList();
        Assert.Equal(["Code by", "Thanks"], groups.Select(g => g.Title));
        var rows = groups[0].Children.OfType<PreferencesRow>().ToList();
        Assert.Equal(["Ada Lovelace", "Grace Hopper", "Linus"], rows.Select(r => r.Title));
        Assert.Equal("mailto:ada@example.org", Assert.IsType<LinkRow>(rows[0]).Uri);
        Assert.Equal("https://example.org/grace", Assert.IsType<LinkRow>(rows[1]).Uri);
        Assert.IsNotType<LinkRow>(rows[2]);

        Navigation(dialog).Pop();
        Row(dialog, "_Legal").Activate();
        Shots.Pump(w);
        var legal = Navigation(dialog).VisiblePage!.GetVisualDescendants().OfType<MarkupLabel>().Single();
        Assert.StartsWith("© 2026 Example\n\nThis application comes with absolutely no warranty.", string.Concat(legal.Inlines!.OfType<Run>().Select(r => r.Text)));
        Assert.Equal("https://opensource.org/licenses/mit-license.php", Assert.Single(legal.Links).Uri);

        // A link the dialog's handler deals with does not open.
        string? activated = null;
        dialog.LinkActivated += (_, e) =>
        {
            activated = e.Uri;
            e.Handled = true;
        };
        legal.Activate(legal.Links[0].Uri);
        Assert.Equal("https://opensource.org/licenses/mit-license.php", activated);
        w.Close();
    }

    [AvaloniaFact]
    public void Release_Notes_And_Debug_Info_Have_Pages()
    {
        var (w, dialog) = Show(new AboutDialog
        {
            ApplicationName = "Example",
            Version = "2.0",
            ReleaseNotes = "<p>New <em>things</em>:</p><ul><li>One</li><li>Two</li></ul>",
            DebugInfo = "Version: 2.0",
            IssueUrl = "https://example.org/issues",
        });
        Row(dialog, "What’s _New").Activate();
        Shots.Pump(w);
        var notes = Navigation(dialog).VisiblePage!.GetVisualDescendants().OfType<SelectableTextBlock>().Single();
        var text = string.Concat(notes.Inlines!.Select(i => i is Run r ? r.Text : "\n"));
        Assert.Equal("Version 2.0\nNew things:\n• One\n• Two", text);

        Navigation(dialog).Pop();
        Assert.IsType<LinkRow>(Row(dialog, "_Report an Issue"));
        Row(dialog, "_Troubleshooting").Activate();
        Shots.Pump(w);
        Row(dialog, "_Debugging Information").Activate();
        Shots.Pump(w);
        Assert.Equal("debuginfo", Navigation(dialog).VisiblePage!.NavigationTag);
        Assert.Contains(Navigation(dialog).VisiblePage!.GetVisualDescendants().OfType<SelectableTextBlock>(), t => t.Text == "Version: 2.0");
        w.Close();
    }

    [AvaloniaFact]
    public void Link_Row_Opens_Its_Link_And_Is_Visited()
    {
        var row = new LinkRow { Title = "Website", Uri = "https://example.org" };
        var w = Shots.Show(new ListBox { Classes = { "boxed-list" }, Width = 360, Items = { row } });
        Assert.Contains("link", Css.GetCssNode(row)!.Classes);
        Assert.True(row.IsActivatable);
        string? opened = null;
        row.LinkActivated += (_, e) =>
        {
            opened = e.Uri;
            e.Handled = true;
        };
        row.Activate();
        Assert.Equal("https://example.org", opened);
        Assert.True(row.IsVisited);
        Assert.True(Css.GetCssNode(row)!.States.HasFlag(CssEngine.Selectors.CssStates.Visited));
        Assert.Equal(AutomationControlType.Hyperlink, ControlAutomationPeer.CreatePeerForElement(row).GetAutomationControlType());
        w.Close();
    }

    [AvaloniaFact]
    public void Markup_Label_Shows_Markup_Or_The_Text_When_It_Does_Not_Parse()
    {
        var label = new MarkupLabel { Markup = "<i>Ita</i> &amp; <a href=\"https://example.org\">link</a>" };
        var w = Shots.Show(label);
        var runs = label.Inlines!.OfType<Run>().ToList();
        Assert.Equal(["Ita", " & ", "link"], runs.Select(r => r.Text));
        Assert.Equal(FontStyle.Italic, runs[0].FontStyle);
        Assert.Equal((6, 4, "https://example.org"), Assert.Single(label.Links));
        Assert.NotNull(runs[2].Foreground);

        label.Markup = "a < b";
        Shots.Pump(w);
        Assert.Equal("a < b", label.Text);
        Assert.Empty(label.Links);
        w.Close();
    }
}
