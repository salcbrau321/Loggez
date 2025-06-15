using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Loggez.Core.Enums;
using Loggez.Core.Models;
using Loggez.UI.ViewModels;
using Loggez.UI.ViewModels.Tabs.Search;

namespace Loggez.Core.Interfaces;

public interface IIndexService
{
    IndexState IndexState { get; }
    
    Task BuildIndexAsync(IEnumerable<LogFile> logs, IProgress<int> progress, CancellationToken ct = default);

    Task<IReadOnlyList<SearchResult>> SearchAsync(string query, DateTime? from, DateTime? to, CancellationToken ct = default);
}