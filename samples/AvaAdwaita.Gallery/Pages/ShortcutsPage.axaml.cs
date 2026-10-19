using Avalonia.Controls;
using AvaAdwaita.Controls;

namespace AvaAdwaita.Gallery.Pages;

public sealed partial class ShortcutsPage : UserControl
{
    public ShortcutsPage()
    {
        InitializeComponent();
        OpenShortcuts.Click += (_, _) => Shortcuts(many: false).Present(this);
        OpenManyShortcuts.Click += (_, _) => Shortcuts(many: true).Present(this);
    }

    private static ShortcutsDialog Shortcuts(bool many)
    {
        var dialog = new ShortcutsDialog();
        dialog.Add(new ShortcutsSection("General")
        {
            Items =
            {
                new ShortcutsItem("New Window", "<Control>n"),
                new ShortcutsItem("Open", "<Control>o"),
                new ShortcutsItem("Quit", "<Control>q") { Subtitle = "Closes every window" },
                new ShortcutsItem("Keyboard Shortcuts", "<Control>question"),
            },
        });
        dialog.Add(new ShortcutsSection("Editing")
        {
            Items =
            {
                new ShortcutsItem("Undo", "<Control>z"),
                new ShortcutsItem("Redo", "<Control><Shift>z <Control>y"),
                new ShortcutsItem("Copy", "<Control>c"),
                new ShortcutsItem("Paste", "<Control>v"),
            },
        });
        dialog.Add(new ShortcutsSection("Navigation")
        {
            Items =
            {
                new ShortcutsItem("Go Back", "<Alt>Left") { Direction = Avalonia.Media.FlowDirection.LeftToRight },
                new ShortcutsItem("Go Back", "<Alt>Right") { Direction = Avalonia.Media.FlowDirection.RightToLeft },
                new ShortcutsItem("Switch to Tab", "<Alt>1...9"),
                new ShortcutsItem("Command Palette", "<Control>k+<Control>p"),
                new ShortcutsItem("Toggle Sidebar", string.Empty),
            },
        });
        if (many)
        {
            // Four titled sections and twenty shortcuts bring the section buttons.
            foreach (var (title, count) in new[] { ("View", 5), ("Tabs", 4) })
            {
                var section = new ShortcutsSection(title);
                for (var i = 1; i <= count; i++)
                {
                    section.Add(new ShortcutsItem($"{title} Action {i}", $"<Control><Alt>{i}"));
                }

                dialog.Add(section);
            }
        }

        return dialog;
    }
}
