using System.Collections.Generic;
using Loggez.Core.Models;

namespace Loggez.Core.Interfaces;

public interface ILogParser
{
    List<LogEntry> Parse(string filePath, string[] rawLines);
}