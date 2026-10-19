using Avalonia.Controls;
using AvaAdwaita.Controls;
using NavigationPage = AvaAdwaita.Controls.NavigationPage;

namespace AvaAdwaita.Gallery.Pages;

public sealed partial class DialogsPage : UserControl
{
    public DialogsPage()
    {
        InitializeComponent();
        OpenDialog.Click += (_, _) => Sample(DialogPresentationMode.Auto).Present(this);
        OpenSheet.Click += (_, _) => Sample(DialogPresentationMode.BottomSheet).Present(this);
        OpenPreferences.Click += (_, _) => Preferences().Present(this);
        OpenAbout.Click += (_, _) => About().Present(this);
        OpenAlert.Click += async (_, _) => Show(await Alert(false).ChooseAsync(this));
        OpenStackedAlert.Click += async (_, _) => Show(await Alert(true).ChooseAsync(this));
        OpenEntryAlert.Click += async (_, _) =>
        {
            var entry = new TextBox { PlaceholderText = "Name" };
            var alert = new AlertDialog { Heading = "Rename File", CloseResponse = "cancel", DefaultResponse = "rename", ExtraChild = entry };
            alert.AddResponse("cancel", "_Cancel");
            alert.AddResponse("rename", "_Rename", ResponseAppearance.Suggested);
            Show(await alert.ChooseAsync(this) + (entry.Text is { Length: > 0 } t ? $" ({t})" : string.Empty));
        };
    }

    private static AboutDialog About()
    {
        var dialog = new AboutDialog
        {
            ApplicationIcon = "help-about-symbolic",
            ApplicationName = "AvaAdwaita Gallery",
            DeveloperName = "The AvaAdwaita Developers",
            Version = "1.0",
            ReleaseNotes = "<p>This release adds the libadwaita controls:</p><ul><li>Rows and preferences</li><li>Dialogs and bottom sheets</li><li>Search</li></ul>",
            Comments = "A gallery of the controls the <b>Adwaita</b> theme styles.",
            Website = "https://example.org",
            IssueUrl = "https://example.org/issues",
            DebugInfo = $"Runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}\nOS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}",
            DebugInfoFilename = "gallery-debug-info.txt",
            Developers = { "Ada Lovelace <ada@example.org>", "Grace Hopper https://example.org/grace" },
            Designers = { "Linus" },
            Copyright = "© 2026 The AvaAdwaita Developers",
            LicenseType = License.MitX11,
        };
        dialog.AddLegalSection("Avalonia", "© The AvaloniaUI Project", License.MitX11);
        dialog.AddAcknowledgementSection(null, ["The GNOME Project"]);
        return dialog;
    }

    private static PreferencesDialog Preferences()
    {
        var dialog = new PreferencesDialog { SearchEnabled = true };
        var details = new ActionRow { Title = "Details", Subtitle = "Opens a subpage", IsActivatable = true };
        details.Activated += (_, _) => dialog.PushSubpage(new NavigationPage
        {
            Title = "Details",
            Content = new ToolbarView
            {
                TopBars = { new HeaderBar() },
                Content = new StatusPage { Classes = { "compact" }, IconName = "dialog-information-symbolic", Title = "A subpage" },
            },
        });
        dialog.Add(new PreferencesPage
        {
            Title = "Appearance",
            IconName = "preferences-system-symbolic",
            Children =
            {
                new PreferencesGroup
                {
                    Title = "Style",
                    Children = { new SwitchRow { Title = "Dark Style" }, new ActionRow { Title = "Font", Subtitle = "Adwaita Sans 11" }, details },
                },
            },
        });
        dialog.Add(new PreferencesPage
        {
            Title = "Behavior",
            IconName = "view-grid-symbolic",
            Children =
            {
                new PreferencesGroup
                {
                    Title = "Windows",
                    Children = { new SwitchRow { Title = "Remember Window Size", IsChecked = true }, new SwitchRow { Title = "Confirm Before Closing" } },
                },
            },
        });
        return dialog;
    }

    private void Show(string response) => AlertResult.Content = $"Chose {response}";

    private static AlertDialog Alert(bool discard)
    {
        var alert = new AlertDialog
        {
            Heading = "Save Changes?",
            Body = "Unsaved changes will be lost.",
            CloseResponse = "cancel",
            DefaultResponse = "save",
        };
        alert.AddResponse("cancel", "_Cancel");
        if (discard)
        {
            alert.AddResponse("discard", "_Discard", ResponseAppearance.Destructive);
        }

        alert.AddResponse("save", "_Save", ResponseAppearance.Suggested);
        return alert;
    }

    private static Dialog Sample(DialogPresentationMode mode) => new()
    {
            Title = "Dialog",
            ContentWidth = 360,
            PresentationMode = mode,
            Content = new ToolbarView
            {
                TopBars = { new HeaderBar() },
                Content = new StatusPage { Classes = { "compact" }, IconName = "dialog-information-symbolic", Title = "A dialog", Description = "Escape or the close button closes it" },
            },
        };
}
