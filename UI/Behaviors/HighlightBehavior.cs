using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Utils;

namespace Loggez.UI.Behaviors;

public class HighlightBehavior : Behavior<TextEditor>
{
    public static readonly StyledProperty<string> SearchQueryProperty =
        AvaloniaProperty.Register<HighlightBehavior, string>(nameof(SearchQuery));

    public string SearchQuery
    {
        get => GetValue(SearchQueryProperty);
        set => SetValue(SearchQueryProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.TextArea.TextView.LineTransformers.Add(_colorizer);
        this.GetObservable(SearchQueryProperty).Subscribe(_ => RefreshHighlights());
        AssociatedObject.DoubleTapped += OnDoubleTapped; 
    }

    protected override void OnDetaching()
    {
        AssociatedObject.DoubleTapped -= OnDoubleTapped;
        AssociatedObject.TextArea.TextView.LineTransformers.Remove(_colorizer);
        base.OnDetaching();
    }

    private readonly SearchColorizer _colorizer = new();

    private void RefreshHighlights()
    {
        _colorizer.SearchTerm = SearchQuery ?? string.Empty;
        AssociatedObject.TextArea.TextView.Redraw();
    }

    private void OnDoubleTapped(object? sender, RoutedEventArgs e)
    {
        if (AssociatedObject.Document is TextDocument doc &&
            AssociatedObject.TextArea.Caret.Line >= 1 &&
            AssociatedObject.TextArea.Caret.Line <= doc.LineCount)
        {
            var line = AssociatedObject.TextArea.Caret.Line;
            var offset = doc.GetOffset(line, 1);
            AssociatedObject.TextArea.Caret.Offset = offset;
            AssociatedObject.TextArea.Caret.BringCaretToView();
        }
    }
}