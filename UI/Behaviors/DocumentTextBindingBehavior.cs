using Avalonia;
using Avalonia.Data;
using Avalonia.Xaml.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.Utils;

namespace Loggez.UI.Behaviors;

public class DocumentTextBindingBehavior : Behavior<TextEditor>
{
    private TextEditor? _editor;

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<DocumentTextBindingBehavior, string?>(
            nameof(Text), defaultBindingMode: BindingMode.TwoWay);

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        _editor = AssociatedObject;
        if (_editor != null)
        {
            _editor.TextChanged += (_, __) =>
            {
                Text = _editor.Document?.Text;
            };
            this.GetObservable(TextProperty)
                .Subscribe(newText =>
                {
                    if (_editor?.Document != null && newText != null)
                    {
                        var co = _editor.CaretOffset;
                        _editor.Document.Text = newText;
                        _editor.CaretOffset = co;
                    }
                });
        }
    }

    protected override void OnDetaching()
    {
        if (_editor != null)
            _editor.TextChanged -= (_, __) => { };
        base.OnDetaching();
    }
}