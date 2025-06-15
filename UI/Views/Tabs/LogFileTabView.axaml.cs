using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Loggez.UI.Views.Tabs;

public partial class LogFileTabView : UserControl
{
    public LogFileTabView()
    {
        InitializeComponent();
    }
    
    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}