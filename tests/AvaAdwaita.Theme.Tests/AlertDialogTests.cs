using System.Linq;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaAdwaita.Controls;
using AvaAdwaita.Controls.Primitives;
using AvaAdwaita.Nodes;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>AvaAdwaita.Controls' AlertDialog: its message area, how its responses lay out, and choosing one.</summary>
public class AlertDialogTests
{
    private static AlertDialog Make(bool destructive = false)
    {
        var alert = new AlertDialog { Heading = "Save Changes?", Body = "Unsaved changes will be lost.", CloseResponse = "cancel", DefaultResponse = "save" };
        alert.AddResponse("cancel", "_Cancel");
        if (destructive)
        {
            alert.AddResponse("discard", "_Discard", ResponseAppearance.Destructive);
        }

        alert.AddResponse("save", "_Save", ResponseAppearance.Suggested);
        return alert;
    }

    private static Window Show(out Button opener)
    {
        opener = new Button { Content = "Open" };
        var w = Shots.Show(opener, 640, 480);
        AdwaitaEnvironment.SuppressTransitions(w);
        return w;
    }

    [AvaloniaFact]
    public void Two_Responses_Sit_Side_By_Side_And_Three_Stack()
    {
        var w = Show(out var opener);
        var alert = Make();
        _ = alert.ChooseAsync(opener);
        Shots.Pump(w);
        Assert.Contains("alert", Css.GetCssNode(alert)!.Classes);
        Assert.Equal(300, alert.Sheet!.Bounds.Width);
        var area = alert.GetVisualDescendants().OfType<AlertResponses>().Single();
        Assert.DoesNotContain("compact", Css.GetCssNode(area)!.Classes);
        var buttons = area.Children.OfType<Button>().ToList();
        Assert.Equal(buttons[0].Bounds.Top, buttons[1].Bounds.Top);
        Assert.Contains("suggested-action", Css.GetCssNode(buttons[1])!.Classes);
        var message = alert.GetVisualDescendants().Select(v => Css.GetCssNode((Avalonia.StyledElement)v)).First(n => n is not null && n.Classes.Contains("message-area"))!;
        Assert.Contains("has-heading", message.Classes);
        Assert.Contains("has-body", message.Classes);
        alert.ForceClose();
        Shots.Pump(w);

        var stacked = Make(destructive: true);
        _ = stacked.ChooseAsync(opener);
        Shots.Pump(w);
        area = stacked.GetVisualDescendants().OfType<AlertResponses>().Single();
        Assert.Contains("compact", Css.GetCssNode(area)!.Classes);

        // Stacked, the first response is at the bottom.
        buttons = area.Children.OfType<Button>().ToList();
        Assert.True(buttons[0].Bounds.Top > buttons[2].Bounds.Top);

        // Preferring the wide layout keeps them side by side.
        stacked.PreferWideLayout = true;
        Shots.Pump(w);
        Assert.DoesNotContain("compact", Css.GetCssNode(area)!.Classes);
        Assert.True(stacked.Sheet!.Bounds.Width > 300);
        w.Close();
    }

    [AvaloniaFact]
    public void Clicking_A_Response_Chooses_It()
    {
        var w = Show(out var opener);
        var alert = Make();
        string? raised = null;
        alert.Response += (_, id) => raised = id;
        var task = alert.ChooseAsync(opener);
        Shots.Pump(w);
        var save = alert.GetVisualDescendants().OfType<AlertResponses>().Single().Children.OfType<Button>().Last();
        save.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Shots.Pump(w);
        Assert.True(task.IsCompleted);
        Assert.Equal("save", task.Result);
        Assert.Equal("save", raised);
        Assert.Null(alert.GetVisualParent());
        w.Close();
    }

    [AvaloniaFact]
    public void Escape_Chooses_The_Close_Response()
    {
        var w = Show(out var opener);
        var alert = Make();
        var task = alert.ChooseAsync(opener);
        Shots.Pump(w);
        w.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        Shots.Pump(w);
        Assert.True(task.IsCompleted);
        Assert.Equal("cancel", task.Result);
        w.Close();
    }

    [AvaloniaFact]
    public void Peer_Is_Named_By_The_Heading()
    {
        var w = Show(out var opener);
        var alert = Make();
        _ = alert.ChooseAsync(opener);
        Shots.Pump(w);
        var peer = ControlAutomationPeer.CreatePeerForElement(alert);
        Assert.Equal("Save Changes?", peer.GetName());
        Assert.Equal("Unsaved changes will be lost.", peer.GetHelpText());
        alert.ForceClose();
        w.Close();
    }
}
