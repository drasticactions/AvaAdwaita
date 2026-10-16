using System;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.VisualTree;

namespace AvaAdwaita.Presenters;

/// <summary>
/// GtkCalendar's year navigation, which Avalonia's calendar lacks: a button with <see cref="StepProperty"/> moves
/// the calendar it is in by that many years, within its display range.
/// </summary>
public static class CalendarYears
{
    /// <summary>The number of years a button moves its calendar.</summary>
    public static readonly AttachedProperty<int> StepProperty =
        AvaloniaProperty.RegisterAttached<Button, int>("Step", typeof(CalendarYears));

    /// <summary>The calendar's month name, as GtkCalendar's month label.</summary>
    public static readonly IValueConverter MonthName = new FuncValueConverter<DateTime, string>(d => CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(d.Month));

    /// <summary>
    /// The calendar's year, as GtkCalendar's year label: in the culture's own era where its calendar counts years
    /// differently but keeps the Gregorian months ("พ.ศ. 2569" in Thai, as GTK's Thai translation asks with %EY).
    /// </summary>
    public static readonly IValueConverter YearName = new FuncValueConverter<DateTime, string>(d => YearText(d, CultureInfo.CurrentCulture));

    /// <summary>The year label's text for <paramref name="d"/> in <paramref name="culture"/>.</summary>
    internal static string YearText(DateTime d, CultureInfo culture)
    {
        var calendar = culture.Calendar;
        if (calendar is GregorianCalendar || d < calendar.MinSupportedDateTime || d > calendar.MaxSupportedDateTime
            || calendar.GetMonth(d) != d.Month || calendar.GetDayOfMonth(d) != d.Day)
        {
            return d.Year.ToString(CultureInfo.InvariantCulture);
        }

        return d.ToString("gg yyyy", culture);
    }

    /// <summary>
    /// A day's abbreviated name ("Sun"), as GtkCalendar's, from the shortest name ("Su") Avalonia gives: the C
    /// library's, as GTK takes it, where there is one.
    /// </summary>
    public static readonly IValueConverter DayName = new FuncValueConverter<string?, string?>(s =>
    {
        var culture = CultureInfo.CurrentCulture;
        var f = culture.DateTimeFormat;
        var i = Array.IndexOf(f.ShortestDayNames, s);
        return i < 0 ? s : LocaleDayNames.Abbreviated(culture)?[i] ?? f.AbbreviatedDayNames[i];
    });

    /// <summary>Every month name, which size the month label as GtkCalendar's stack of them.</summary>
    public static string[] MonthNames { get; } = Enumerable.Range(1, 12).Select(m => CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m)).ToArray();

    static CalendarYears()
    {
        StepProperty.Changed.AddClassHandler<Button>((b, e) =>
        {
            b.Click -= OnClick;
            if (e.NewValue is int and not 0)
            {
                b.Click += OnClick;
            }
        });
    }

    /// <summary>Gets <see cref="StepProperty"/>.</summary>
    public static int GetStep(Button b) => b.GetValue(StepProperty);

    /// <summary>Sets <see cref="StepProperty"/>.</summary>
    public static void SetStep(Button b, int value) => b.SetValue(StepProperty, value);

    private static void OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not Button b || b.FindAncestorOfType<Avalonia.Controls.Calendar>() is not { } calendar)
        {
            return;
        }

        var target = calendar.DisplayDate.AddYears(GetStep(b));
        if (calendar.DisplayDateStart is { } start && target < start)
        {
            target = start;
        }

        if (calendar.DisplayDateEnd is { } end && target > end)
        {
            target = end;
        }

        calendar.DisplayDate = target;
    }
}
