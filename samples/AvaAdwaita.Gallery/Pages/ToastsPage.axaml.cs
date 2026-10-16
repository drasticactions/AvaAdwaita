using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;

namespace AvaAdwaita.Gallery.Pages;

public sealed partial class ToastsPage : UserControl
{
    private WindowNotificationManager? _toasts;

    public ToastsPage() => InitializeComponent();

    // The manager is made before the first toast: one shown before its template is applied is lost.
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (_toasts is null && TopLevel.GetTopLevel(this) is { } top)
        {
            _toasts = new WindowNotificationManager(top) { MaxItems = 3 };
        }
    }

    private void OnShowToast(object? sender, RoutedEventArgs e) => _toasts?.Show(new Notification("File saved", null));

    private void OnShowError(object? sender, RoutedEventArgs e) => _toasts?.Show(new Notification("Could not save", "The disk is full.", NotificationType.Error));
}
