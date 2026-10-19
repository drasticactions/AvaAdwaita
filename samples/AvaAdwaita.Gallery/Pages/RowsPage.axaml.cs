using Avalonia.Controls;

namespace AvaAdwaita.Gallery.Pages;

public sealed partial class RowsPage : UserControl
{
    private int _activations;

    public RowsPage()
    {
        InitializeComponent();
        Activatable.Activated += (_, _) => Activatable.Subtitle = ++_activations == 1 ? "Activated once" : $"Activated {_activations} times";
    }
}
