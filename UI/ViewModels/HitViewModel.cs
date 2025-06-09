namespace Loggez.UI.ViewModels;

using Loggez.Core.Models;
using System;
using System.IO;

public class HitViewModel
{
    public string PreviewBefore { get; }
    public string PreviewMatch  { get; }
    public string PreviewAfter  { get; }
    public string FileName { get; }
    public string FullPath { get; }
    public int LineNumber { get; }
    
    public HitViewModel(LogFile file, LogEntry entry, string query, StringComparison cmp)
    {
        FileName   = Path.GetFileName(file.Path);
        FullPath   = file.Path;
        LineNumber = entry.LineNumber;

        var line = entry.RawLine;
        var idx  = line.IndexOf(query, cmp);
        if (idx < 0)
        {
            PreviewBefore = line;
            PreviewMatch  = "";
            PreviewAfter  = "";
        }
        else
        {
            PreviewBefore = line.Substring(0, idx);
            PreviewMatch  = line.Substring(idx, query.Length);
            PreviewAfter  = line.Substring(idx + query.Length);
        }
    }

}