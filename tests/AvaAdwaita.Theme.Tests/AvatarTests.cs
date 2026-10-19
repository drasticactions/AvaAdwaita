using Avalonia.Automation.Peers;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using AvaAdwaita.Controls;
using AvaAdwaita.Nodes;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>AvaAdwaita.Controls' Avatar: initials, the color from the name, and the custom image.</summary>
public class AvatarTests
{
    [Theory]
    [InlineData("Ada Lovelace", "AL")]
    [InlineData("  linus  ", "L")]
    [InlineData("jean de la fontaine", "JF")]
    [InlineData("élodie", "É")]
    [InlineData("", null)]
    public void Initials_Are_The_First_And_Last_Words(string text, string? initials) =>
        Assert.Equal(initials, Avatar.ExtractInitials(text));

    [AvaloniaFact]
    public void Color_Comes_From_The_Name()
    {
        // GTK gives "Ada Lovelace" avatar.color11 (the oracle's tree).
        var avatar = new Avatar { Text = "Ada Lovelace", ShowInitials = true, Size = 20 };
        var w = Shots.Show(avatar);
        var circle = Css.GetCssNode(avatar)!.Children[0];
        Assert.Equal("avatar", circle.Name);
        Assert.Contains("color11", circle.Classes);
        Assert.Contains("contrasted", circle.Classes);
        Assert.Equal("Avatar of Ada Lovelace", ControlAutomationPeer.CreatePeerForElement(avatar).GetName());

        avatar.Size = 48;
        avatar.Image = new WriteableBitmap(new Avalonia.PixelSize(4, 4), new Avalonia.Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        Shots.Pump(w);
        Assert.DoesNotContain("contrasted", circle.Classes);
        Assert.Contains("image", circle.Classes);
        Assert.Null(avatar.Initials);
        w.Close();
    }
}
