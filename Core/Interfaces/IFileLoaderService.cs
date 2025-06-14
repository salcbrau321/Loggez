using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Loggez.Core.Models;

namespace Loggez.Core.Interfaces;

public interface IFileLoaderService
{
    Task<IReadOnlyList<LogFile>> LoadAsync(IEnumerable<string> inputs, CancellationToken ct = default);
}