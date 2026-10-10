using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace Optimizer.App.Views;

public sealed class NullToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var visible = value is not null && (value is not string s || s.Length > 0);
        return visible ^ Invert ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value is true) ^ Invert ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not true;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not true;
}

/// <summary>Theme brush by resource key (status colors follow the Windows theme; items are rebuilt after a theme switch).</summary>
/// <summary>
/// Foreground from a theme brush, by resource key or by status ("Ok", "Problem", ...). It is set as a resource reference
/// (like {DynamicResource}), so the color follows a theme switch without building the rows again. Icons, text and
/// controls all share TextElement.ForegroundProperty.
/// </summary>
public static class ThemeBrush
{
    public static readonly DependencyProperty KeyProperty = DependencyProperty.RegisterAttached(
        "Key", typeof(string), typeof(ThemeBrush), new PropertyMetadata(null, (d, e) => Apply(d, e.NewValue as string)));

    public static readonly DependencyProperty StatusProperty = DependencyProperty.RegisterAttached(
        "Status", typeof(string), typeof(ThemeBrush), new PropertyMetadata(null, (d, e) => Apply(d, e.NewValue is string s ? ViewModels.StatusIcons.BrushKey(s) : null)));

    public static string? GetKey(DependencyObject d) => (string?)d.GetValue(KeyProperty);
    public static void SetKey(DependencyObject d, string? value) => d.SetValue(KeyProperty, value);
    public static string? GetStatus(DependencyObject d) => (string?)d.GetValue(StatusProperty);
    public static void SetStatus(DependencyObject d, string? value) => d.SetValue(StatusProperty, value);

    private static void Apply(DependencyObject d, string? key)
    {
        if (d is not FrameworkElement element) return;
        if (key is null) element.ClearValue(TextElement.ForegroundProperty);
        else element.SetResourceReference(TextElement.ForegroundProperty, key);
    }
}

public sealed class StatusToSymbolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        ViewModels.StatusIcons.Symbol(value as string ?? "");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>Collection count -> visible when above zero (Invert: visible when empty).</summary>
public sealed class CountToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value is int n && n > 0) ^ Invert ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>Non-empty value -> true (InfoBar.IsOpen bound to an optional message).</summary>
public sealed class NullToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not null && (value is not string s || s.Length > 0);
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}

/// <summary>True when every bound value is true (a row's own "can toggle" and the app-wide "a change may start now").</summary>
public sealed class AllTrueConverter : IMultiValueConverter
{
    public object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture) => values.All(v => v is true);
    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>A layout width to set on a child: 0 (not laid out yet) means automatic, so nothing collapses before the first layout.</summary>
public sealed class LayoutWidthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is double w && w > 0 ? w : double.NaN;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
