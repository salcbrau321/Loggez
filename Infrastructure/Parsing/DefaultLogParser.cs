using System.Collections.Generic;
using Loggez.Core.Interfaces;
using Loggez.Core.Models;

namespace Loggez.Infrastructure.Parsing;

public class DefaultLogParser : ILogParser
{
    public List<LogEntry> Parse(string filePath, string[] rawLines)
    {
        var entries = new List<LogEntry>(rawLines.Length);
        for (int i = 0; i < rawLines.Length; i++)
        {
            entries.Add(new LogEntry(
                sourceFile: filePath,
                lineNumber: i,
                rawLine: rawLines[i]
            ));
        }
        return entries;
    }
}