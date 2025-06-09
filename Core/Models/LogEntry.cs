using System;
using System.Collections.Generic;

namespace Loggez.Core.Models;

public class LogEntry(string sourceFile, int lineNumber, string rawLine, DateTime? timeStamp = null)
{
    public string SourceFile { get; } = sourceFile;
    public int LineNumber { get; } = lineNumber;
    public string RawLine { get; } = rawLine;
    public DateTime? Timestamp = timeStamp; 

    public string? Level                    { get; set; }   
    public string? EntryType                { get; set; }   
    public Dictionary<string,string> Fields { get; } = new();
}