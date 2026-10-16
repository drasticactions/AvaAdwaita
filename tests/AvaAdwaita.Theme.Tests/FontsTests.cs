using Avalonia.Headless.XUnit;
using Avalonia.Media;
using AvaAdwaita.Fonts;
using AvaAdwaita.Markup;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

public class FontsTests
{
    [AvaloniaTheory]
    [InlineData(AdwaitaFontCollection.Sans, "Adwaita Sans")]
    [InlineData(AdwaitaFontCollection.Mono, "Adwaita Mono")]
    public void Bundled_Families_Load(string family, string name)
    {
        Assert.True(FontManager.Current.TryGetGlyphTypeface(new Typeface(family), out var face));
        Assert.Equal(name, face!.FamilyName);
    }

    [AvaloniaFact]
    public void Families_Fall_Back_To_The_Adwaita_Fonts()
    {
        Assert.Equal([AdwaitaFontCollection.Sans], CssConvert.FallbackFamilies);
        Assert.Equal([AdwaitaFontCollection.Mono], CssConvert.MonospaceFallbackFamilies);
        Assert.Equal(2, CssConvert.FontFamily(["No Such Family"]).FamilyNames.Count);
        Assert.Equal(3, CssConvert.FontFamily(["Monospace"]).FamilyNames.Count);
    }
}
