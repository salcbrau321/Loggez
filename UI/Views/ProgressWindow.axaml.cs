using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Loggez.UI.Views;

public partial class ProgressWindow : Window
{
    public ProgressWindow()
    {
        InitializeComponent();
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}