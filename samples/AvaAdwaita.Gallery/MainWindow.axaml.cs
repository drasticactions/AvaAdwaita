using System.Globalization;
using Avalonia.Controls;
using Avalonia.Media;

namespace AvaAdwaita.Gallery;

public partial class MainWindow : Window
{
    /// <summary>Leaves the client area unextended, so the platform's drawn decorations wrap the window (--no-extend).</summary>
    public static bool NoExtend { get; set; }

    public MainWindow()
    {
        InitializeComponent();

        // As GTK does, lay the window out right to left in a right-to-left language.
        if (CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft)
        {
            FlowDirection = FlowDirection.RightToLeft;
        }

        if (NoExtend)
        {
            ExtendClientAreaToDecorationsHint = false;
        }

        SidebarToggle.IsCheckedChanged += (_, _) => View.IsSidebarVisible = SidebarToggle.IsChecked == true;
#if DEBUG
        DebugPanel.Attach(this);
#endif
    }
}
