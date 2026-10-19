using System;
using Avalonia.Controls;

namespace AvaAdwaita.Gallery.Pages;

public sealed partial class FeedbackPage : UserControl
{
    public FeedbackPage()
    {
        InitializeComponent();
        ShowBanner.IsCheckedChanged += (_, _) => Banner.IsRevealed = ShowBanner.IsChecked == true;
        Level.ValueChanged += (_, e) =>
        {
            Continuous.Value = e.NewValue;
            Discrete.Value = Math.Round(e.NewValue * 5);
        };
    }
}
