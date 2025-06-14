using System.ComponentModel;
using Avalonia.Input;
using Avalonia.Threading;
using Loggez.UI.Behaviors;
using Lucene.Net.Util.Packed;

namespace Loggez.UI.Views;

using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Loggez.UI.ViewModels;

public partial class MainWindow : Window
{
    private readonly SearchColorizer _searchColorizer;
    private bool _ignoreDrag = false;
    
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
        this.FindControl<Border>("TitleBar").PointerPressed += TitleBar_PointerPressed;
    }
    
    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.ClickCount == 2 && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _ignoreDrag = true;
            
            this.WindowState = this.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }
        else if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (!_ignoreDrag)
                    BeginMoveDrag(e);
                _ignoreDrag = false;
            }, Avalonia.Threading.DispatcherPriority.Input);
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
    
    private void Resize_Right(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginResizeDrag(WindowEdge.East, e);
    }

    private void Resize_Left(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginResizeDrag(WindowEdge.West, e);
    }

    private void Resize_Top(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginResizeDrag(WindowEdge.North, e);
    }

    private void Resize_Bottom(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginResizeDrag(WindowEdge.South, e);
    }
    
    private void Resize_TopLeft(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginResizeDrag(WindowEdge.NorthWest, e);
    }

    private void Resize_TopRight(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginResizeDrag(WindowEdge.NorthEast, e);
    }

    private void Resize_BottomLeft(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginResizeDrag(WindowEdge.SouthWest, e);
    }

    private void Resize_BottomRight(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginResizeDrag(WindowEdge.SouthEast, e);
    }
}