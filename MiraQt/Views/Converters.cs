using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using MiraQt.Models;

namespace MiraQt.Views;

public sealed class BoolToColorConverter : IValueConverter
{
    public static readonly BoolToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Color.Parse("#27AE60") : Color.Parse("#DA4453");

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class StateToColorConverter : IValueConverter
{
    public static readonly StateToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not SinkState state) return Color.Parse("#7F8C8D");
        return state switch
        {
            SinkState.Streaming        => Color.Parse("#27AE60"),
            SinkState.Error            => Color.Parse("#DA4453"),
            SinkState.Disconnected     => Color.Parse("#7F8C8D"),
            _                          => Color.Parse("#F39C12"), // any "wait-*" / busy state
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
