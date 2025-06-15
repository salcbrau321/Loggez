using System.ComponentModel;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Loggez.UI.Behaviors;
using Lucene.Net.Util.Packed;

namespace Loggez.UI.Views;

using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Loggez.UI.ViewModels;
using Avalonia.VisualTree;

public partial class MainWindow : Window
{
    private bool _ignoreDrag = false;
    
    public MainWindow() => this.InitializeComponent();
    
    public MainWindow(MainWindowViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
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
    
    private void SolutionTree_DoubleTapped(object? sender, RoutedEventArgs e)
    {
        if (!(e.Source is Control sourceControl))
            return;
        
        Control? current = sourceControl;
        while (current is not TreeViewItem && current != null)
        {
            current = current.Parent as Control;
        }

        if (current is TreeViewItem tvi &&
            tvi.DataContext is SolutionFileViewModel fileVm)
        {
            (DataContext as MainWindowViewModel)?
                .OpenLogFileTabCommand.Execute(fileVm);

            e.Handled = true;
        }
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