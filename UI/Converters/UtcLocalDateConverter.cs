using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Loggez.UI.Converters;

public class UtcLocalDateConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dt && dt.Kind == DateTimeKind.Utc)
            return (DateTime?)dt.ToLocalTime();
        if (value is DateTime dt2 && dt2.Kind == DateTimeKind.Unspecified)
            return (DateTime?)DateTime.SpecifyKind(dt2, DateTimeKind.Utc).ToLocalTime();
        return null;
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dt)
            return (DateTime?)dt.ToUniversalTime();
        return null;
    }
}