namespace AvaAdwaita.Animation;

/// <summary>How <see cref="AdwSpringSlide"/> moves the pages.</summary>
public enum AdwSlideMode
{
    /// <summary>The new page covers the old one, which stays in place: AdwNavigationView.</summary>
    Cover,

    /// <summary>Both pages move together: AdwCarousel.</summary>
    Slide,
}
