using System;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;
using Loggez.UI.Behaviors;
using Loggez.UI.ViewModels;
using Loggez.UI.ViewModels.Tabs.Search;

namespace Loggez.UI.Views.Tabs;

public partial class SearchTabView : UserControl
{
    public SearchTabView()
    {
        InitializeComponent();
        this.AttachedToVisualTree += OnViewAttached;
    }
    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    
    private void OnViewAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        // Only run once
        this.AttachedToVisualTree -= OnViewAttached;

        // Now the hits TreeView exists—grab it
        var tree = this.FindControl<TreeView>("HitsTree");
        if (tree != null)
        {
            tree.SelectionChanged += OnHitsSelectionChanged;
        }
    }

    private void OnHitsSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is HitViewModel hit)
        {
            ScrollToCurrentHit(hit);
        }
    }

    private void ScrollToCurrentHit(HitViewModel? hit = null)
    {
        if (hit == null && DataContext is SearchTabViewModel vm)
            hit = vm.SelectedHit;
        if (hit == null) return;

        var editor = this.FindControl<TextEditor>("Editor");
        var doc    = editor?.Document;
        if (editor == null || doc == null) return;

        // clamp line
        int line = Math.Clamp(hit.LineNumber, 1, doc.LineCount);

        // compute the pixel center of that line
        var textView = editor.TextArea.TextView;
        textView.EnsureVisualLines();
        double lh  = textView.DefaultLineHeight;
        double mid = (line - 0.5) * lh;
        double vh  = textView.Bounds.Height;

        // target scroll Y so that mid ends up at vh/2
        double targetY = mid - (vh / 2);
        double maxY    = Math.Max(0, textView.DocumentHeight - vh);
        targetY = Math.Clamp(targetY, 0, maxY);

        // grab the inner ScrollViewer
        var sv = editor.GetVisualDescendants()
            .OfType<ScrollViewer>()
            .FirstOrDefault();
        if (sv != null)
        {
            // either via CLR property:
            sv.Offset = new Vector(sv.Offset.X, targetY);

            // or via styled-property:
            // sv.SetValue(ScrollViewer.OffsetProperty, new Vector(sv.Offset.X, targetY));
        }

        // move caret onto the match
        var offset = doc.GetOffset(line, 1);
        editor.TextArea.Caret.Offset = offset;
        editor.TextArea.Caret.BringCaretToView();
    }
}