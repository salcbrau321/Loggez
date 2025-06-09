using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Controls.Documents;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Loggez.UI.Converters
{
    public class HighlightInlineConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text  = (value as string) ?? "";
            var query = (parameter as string) ?? "";
            var cmp   = StringComparison.OrdinalIgnoreCase;
            var inlines = new List<Inline>();

            if (string.IsNullOrWhiteSpace(query))
            {
                inlines.Add(new Run(text));
                return inlines;
            }

            int idx = 0;
            while (true)
            {
                int match = text.IndexOf(query, idx, cmp);
                if (match < 0) break;

                if (match > idx)
                    inlines.Add(new Run(text.Substring(idx, match - idx)));

                inlines.Add(new Run(text.Substring(match, query.Length))
                {
                    Foreground = Brushes.Red
                });

                idx = match + query.Length;
            }

            if (idx < text.Length)
                inlines.Add(new Run(text.Substring(idx)));

            return inlines;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}