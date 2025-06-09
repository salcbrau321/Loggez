using System;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using Avalonia.Media;

namespace Loggez.UI.Behaviors
{
    public class SearchColorizer : DocumentColorizingTransformer
    {
        public string SearchTerm { get; set; } = "";

        protected override void ColorizeLine(DocumentLine line)
        {
            if (string.IsNullOrEmpty(SearchTerm))
                return;

            var text            = CurrentContext.Document.GetText(line.Offset, line.Length);
            var comparison      = StringComparison.InvariantCultureIgnoreCase;
            int lineStartOffset = line.Offset;
            int idx             = 0;

            while ((idx = text.IndexOf(SearchTerm, idx, comparison)) != -1)
            {
                ChangeLinePart(
                    lineStartOffset + idx,
                    lineStartOffset + idx + SearchTerm.Length,
                    element => element.TextRunProperties.SetForegroundBrush(Brushes.Red)
                );
                idx += SearchTerm.Length;
            }
        }
    }
}