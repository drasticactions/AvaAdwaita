using System;
using Avalonia.Controls;

namespace AvaAdwaita.Gallery.Pages;

public sealed partial class PickersPage : UserControl
{
    public PickersPage()
    {
        InitializeComponent();
        Time.SelectedTime = DateTime.Now.TimeOfDay;
    }
}
