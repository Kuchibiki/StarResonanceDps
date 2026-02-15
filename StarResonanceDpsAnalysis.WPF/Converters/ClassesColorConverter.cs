using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using StarResonanceDpsAnalysis.Core.Models;

namespace StarResonanceDpsAnalysis.WPF.Converters;

internal sealed class ClassesColorConverter : IValueConverter
{
    // Use static cache so we can update colors globally
    private static readonly Dictionary<Classes, Brush> _brushCache = new();

    public static void UpdateColor(Classes classes, Color color)
    {
        if (_brushCache.TryGetValue(classes, out var brush) && brush is SolidColorBrush solidBrush && !solidBrush.IsFrozen)
        {
            solidBrush.Color = color;
        }
        else
        {
            _brushCache[classes] = new SolidColorBrush(color);
        }
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Classes classes) return null;

        if (_brushCache.TryGetValue(classes, out var cached) && cached is not null)
            return cached;

        var app = Application.Current;
        var keysToTry = new object?[]
        {
            value,
            $"Classes{value}Brush",
            $"{value}Brush",
            $"Classes{value}Color",
            $"{value}Color"
        };

        foreach (var key in keysToTry)
        {
            var resource = app?.TryFindResource(key!);
            if (resource is Brush brush)
            {
                // If the resource brush is frozen, we can't update it later.
                // For customizability, we might want to wrap it or clone it if it's solid.
                if (brush is SolidColorBrush solidBrush && solidBrush.IsFrozen)
                {
                    var clone = solidBrush.Clone();
                    _brushCache[classes] = clone;
                    return clone;
                }

                _brushCache[classes] = brush;
                return brush;
            }

            if (resource is Color color)
            {
                var solidBrush = new SolidColorBrush(color);
                // Do not freeze if we want to allow updates
                // if (solidBrush.CanFreeze)
                // {
                //     solidBrush.Freeze();
                // }

                _brushCache[classes] = solidBrush;
                return solidBrush;
            }
        }

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
