using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Loggez.Core.Interfaces;
using Loggez.Core.Models;
using Loggez.UI;

namespace Loggez.Infrastructure.FileLoading;

public class FileLoaderService : IFileLoaderService
{
    private readonly ILogParser _parser;

    public FileLoaderService(ILogParser parser)
        => _parser = parser;

    public async Task<IReadOnlyList<LogFile>> LoadAsync(IEnumerable<string> inputs, CancellationToken cancellationToken = default)
    {
        var paths = new List<string>();

        foreach (var input in inputs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (Directory.Exists(input))
            {
                var files = Directory
                    .EnumerateFiles(input, "*.*", SearchOption.AllDirectories)
                    .Where(f => Settings.SupportedExtensions
                                 .Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase));
                paths.AddRange(files);
            }
            else if (Path.GetExtension(input)
                     .Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(temp);

                using var zip = ZipFile.OpenRead(input);
                foreach (var entry in zip.Entries
                    .Where(e => Settings.SupportedExtensions
                        .Contains(Path.GetExtension(e.Name), StringComparer.OrdinalIgnoreCase)))
                {
                    var dest = Path.Combine(temp, entry.Name);
                    entry.ExtractToFile(dest);
                    paths.Add(dest);
                }
            }
            else if (File.Exists(input))
            {
                paths.Add(input);
            }
        }

        var logs = new List<LogFile>();
        await Task.Run(() =>
        {
            foreach (var path in paths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var lf = new LogFile(path, _parser);
                lf.Parse();
                logs.Add(lf);
            }
        }, cancellationToken);

        return logs;
    }
}

