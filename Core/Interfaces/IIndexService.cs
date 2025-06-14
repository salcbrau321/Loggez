using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Loggez.Core.Models;
using Loggez.UI.ViewModels;

namespace Loggez.Core.Interfaces;

public interface IIndexService
{
    Task BuildIndexAsync(IEnumerable<LogFile> logs, IProgress<int> progress, CancellationToken ct = default);

    Task<IReadOnlyList<HitViewModel>> SearchAsync(string query, DateTime? from, DateTime? to, CancellationToken ct = default);
}