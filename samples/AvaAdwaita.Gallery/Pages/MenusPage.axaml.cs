using Avalonia.Controls;

namespace AvaAdwaita.Gallery.Pages;

public sealed partial class MenusPage : UserControl
{
    public MenusPage()
    {
        InitializeComponent();
        PopoverDone.Click += (_, _) => OpenPopover.Flyout?.Hide();
    }
}
