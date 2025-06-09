using System.ComponentModel;
using Avalonia.Threading;
using Loggez.UI.Behaviors;

namespace Loggez.UI.Views;

using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Loggez.UI.ViewModels;

public partial class MainWindow : Window
{
    private readonly SearchColorizer _searchColorizer;
    
    public MainWindow() => this.InitializeComponent();
    
    public MainWindow(MainWindowViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        _searchColorizer =  new SearchColorizer();
        Editor.TextArea.TextView.LineTransformers.Add(_searchColorizer);
        if (DataContext is INotifyPropertyChanged viewModel)
        {
            vm.PropertyChanged += ViewModel_PropertyChanged;
        }
    }
    
    private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.SearchQuery))
        {
            RefreshHighlights();
        } 
        if (e.PropertyName == nameof(MainWindowViewModel.SelectedHit))
        {
            Dispatcher.UIThread.InvokeAsync(ScrollToCurrentHit, DispatcherPriority.Background);
        }
    }
    
    private void ScrollToCurrentHit()
    {
        var vm = DataContext as MainWindowViewModel;
        var hit = vm?.SelectedHit;
        var doc = Editor.Document; 
        if (hit == null || doc == null)
            return;
        if (hit.LineNumber < 1 || hit.LineNumber > doc.LineCount)
            return;
        var offset = doc.GetOffset(hit.LineNumber, 1);
        Editor.TextArea.Caret.Offset = offset;
        Editor.TextArea.Caret.BringCaretToView();
    }
    
    private void RefreshHighlights()
    {
        var vm = (MainWindowViewModel)DataContext;
        _searchColorizer.SearchTerm = vm.SearchQuery ?? string.Empty;
        Editor.TextArea.TextView.Redraw();  
    }
}