using System;
using System.Collections.Generic;
using System.IO;
using Loggez.Core.Interfaces;

namespace Loggez.Core.Models;

public class LogFile
{
    private readonly ILogParser _parser;
    
    public string Path { get; }
    public string[] RawLines { get; }
    public DateTime CreatedDate { get; }
    public DateTime ModifiedDate { get; } 
    public List<LogEntry> Entries { get; private set; } = new();

    public LogFile(string path, ILogParser parser)
    {
        this.Path = path ?? throw new ArgumentNullException(nameof(path));
        this.RawLines = File.ReadAllLines(Path);
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        var info = new FileInfo(Path);
        CreatedDate = info.CreationTime.ToUniversalTime(); 
        ModifiedDate = info.LastWriteTime.ToUniversalTime();
    }

    public void Parse()
    {
       this. Entries = _parser.Parse(this.Path, this.RawLines);
    }
}