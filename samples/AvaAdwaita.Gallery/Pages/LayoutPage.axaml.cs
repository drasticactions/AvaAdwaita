using Avalonia.Controls;

namespace AvaAdwaita.Gallery.Pages;

public sealed partial class LayoutPage : UserControl
{
    public LayoutPage()
    {
        InitializeComponent();
        NavCollapsed.IsCheckedChanged += (_, _) => NavSplit.IsCollapsed = NavCollapsed.IsChecked == true;
        Folders.SelectionChanged += (_, _) => NavSplit.ShowContent = true;
        OpenMessage.Click += (_, _) => Nav.PushByTag("message");
        WrapJustify.SelectionChanged += (_, _) => Wrap.Justify = (AvaAdwaita.Controls.JustifyMode)WrapJustify.SelectedIndex;
        WrapCentered.IsCheckedChanged += (_, _) => Wrap.Align = WrapCentered.IsChecked == true ? 0.5 : 0;
        Collapsed.IsCheckedChanged += (_, _) => Split.IsCollapsed = Collapsed.IsChecked == true;
        ShowSidebar.IsCheckedChanged += (_, _) => Split.ShowSidebar = ShowSidebar.IsChecked == true;
        Split.PropertyChanged += (_, e) =>
        {
            if (e.Property == AvaAdwaita.Controls.OverlaySplitView.ShowSidebarProperty)
            {
                ShowSidebar.IsChecked = Split.ShowSidebar;
            }
        };
    }
}
