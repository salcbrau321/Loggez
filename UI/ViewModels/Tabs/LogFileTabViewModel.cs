using System;
using System.IO;
using Avalonia.Controls.Shapes;
using AvaloniaEdit.Document;
using Path = System.IO.Path;

namespace Loggez.UI.ViewModels.Tabs;

public class LogFileTabViewModel : TabViewModelBase
{
    public string FilePath { get; }
    public TextDocument Document { get; }
    public event EventHandler? RequestClose;
    
    public LogFileTabViewModel(string path) : base(Path.GetFileName(path))
    {
        FilePath = path;
        var text = File.ReadAllText(path);
        Document = new TextDocument(text);
    }
    
    protected override void RunClose()
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
    }
}