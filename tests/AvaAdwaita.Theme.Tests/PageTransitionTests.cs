using System;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using AvaAdwaita.Animation;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>libadwaita's spring page transitions: AdwNavigationView's cover slide and AdwCarousel's slide.</summary>
/// <remarks>Alone, as the parity tests suppress transitions for the whole process while they run.</remarks>
[Collection(nameof(PageTransitionCollection))]
public class PageTransitionTests
{
    [AvaloniaFact]
    public void Navigation_And_Carousel_Use_The_Springs()
    {
        var nav = new NavigationPage();
        var carousel = new Carousel();
        var w = Shots.Show(new StackPanel { Children = { nav, carousel } });
        var push = Assert.IsType<AdwSpringSlide>(nav.PageTransition);
        Assert.Equal((AdwSlideMode.Cover, 1.0, 1.0, 1000.0), (push.Mode, push.DampingRatio, push.Mass, push.Stiffness));
        var slide = Assert.IsType<AdwSpringSlide>(carousel.PageTransition);
        Assert.Equal((AdwSlideMode.Slide, 1.0, 0.5, 500.0), (slide.Mode, slide.DampingRatio, slide.Mass, slide.Stiffness));
        w.Close();
    }

    [AvaloniaFact]
    public void A_Push_Slides_The_New_Page_Over_The_Old()
    {
        // An earlier test's color scheme switch pauses transitions for 250 ms; end the pause.
        AdwaitaEnvironment.ResumeTransitions();
        var from = new Border { Background = Brushes.Red };
        var to = new Border { Background = Brushes.Blue, IsVisible = false };
        var w = Shots.Show(new Grid { Width = 300, Height = 200, Children = { from, to } });
        var task = new AdwSpringSlide().Start(from, to, forward: true, CancellationToken.None);

        // Watch a frame mid-way: the new page between the edge and its place, the old one still and clipped.
        var seen = false;
        var deadline = Environment.TickCount64 + 3000;
        while (!task.IsCompleted && Environment.TickCount64 < deadline)
        {
            Frames.Run(16);
            if (to.RenderTransform is TranslateTransform { X: > 0 and < 300 } && from.Clip is not null && from.RenderTransform is TranslateTransform { X: 0 })
            {
                seen = true;
            }
        }

        Assert.True(task.IsCompleted, "the transition did not finish");
        Assert.True(seen, "no frame showed the new page sliding in over the old one");
        Assert.False(from.IsVisible);
        Assert.True(to.IsVisible);
        Assert.Null(to.RenderTransform);
        Assert.Null(from.Clip);
        w.Close();
    }
}

[CollectionDefinition(nameof(PageTransitionCollection), DisableParallelization = true)]
public class PageTransitionCollection;
