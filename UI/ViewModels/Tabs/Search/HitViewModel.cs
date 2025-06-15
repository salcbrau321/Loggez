using Loggez.Core.Models;
using System;
using System.IO;

namespace Loggez.UI.ViewModels.Tabs.Search;

public class HitViewModel
{
    public string PreviewBefore { get; }
    public string PreviewMatch  { get; }
    public string PreviewAfter  { get; }
    public string FileName { get; }
    public string FullPath { get; }
    public int LineNumber { get; }
    public int DisplayLineNumber => LineNumber + 1;
    
    public DateTime FileDate { get; }
    
    public HitViewModel(SearchResult result)
    {
        if (result is null) 
            throw new ArgumentNullException(nameof(result));

        FileName   = result.FileName;
        FullPath   = result.FullPath;
        LineNumber = result.LineNumber;
        FileDate = result.FileDate;
        
        // grab the full line (never null)
        var line = result.LineContent ?? string.Empty;
        var idx  = result.MatchIndex;
        var len  = result.MatchLength;

        // if the match index is invalid, just show the whole line as “before”
        if (idx < 0 || len <= 0 || idx + len > line.Length)
        {
            PreviewBefore = line;
            PreviewMatch  = string.Empty;
            PreviewAfter  = string.Empty;
        }
        else
        {
            PreviewBefore = line.Substring(0, idx);
            PreviewMatch  = line.Substring(idx, len);
            PreviewAfter  = line.Substring(idx + len);
        }
    }
}