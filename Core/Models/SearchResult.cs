using System;
using System.Diagnostics;
using System.IO;

namespace Loggez.Core.Models;

public class SearchResult
{
    public string FileName { get; }
    public DateTime FileDate { get; }
    public string FullPath { get; }
    public int LineNumber { get; }
    public string LineContent { get; }
    public int MatchIndex { get; }
    public int MatchLength { get; }
    
    public SearchResult(LogFile file, LogEntry entry, string query, StringComparison cmp)
    {
        FileName   = Path.GetFileName(file.Path);
        FullPath   = file.Path;
        LineNumber = entry.LineNumber;
        LineContent = entry.RawLine;
        MatchIndex = entry.RawLine.IndexOf(query, cmp);
        MatchLength = query.Length;
        FileDate = file.CreatedDate;
    }
}