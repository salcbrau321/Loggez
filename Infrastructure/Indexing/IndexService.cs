using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Loggez.Core.Interfaces;
using Loggez.Core.Models;
using Loggez.UI.ViewModels;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace Loggez.Infrastructure.Indexing;

public class IndexService : IIndexService
{
    private readonly LuceneVersion _version = LuceneVersion.LUCENE_48;
    private readonly Lucene.Net.Store.Directory _directory;
    private readonly Analyzer _analyzer;
    private readonly string _timestampField = "FileTimestamp";
    private readonly string _pathField = "FullPath";
    private readonly string _lineField = "LineNumber";
    private readonly string _rawField = "ContentRaw";
    private readonly string _substrField  = "ContentSubstr";

    private IndexWriter   _writer;
    private IndexSearcher _searcher;
    private List<LogFile> _indexedLogs = new();

    public IndexService(string indexPath, IAnalyzerFactory analyzerFactory)
    {
        System.IO.Directory.CreateDirectory(indexPath);
        _directory = FSDirectory.Open(indexPath);

        _analyzer = analyzerFactory.Create("default");
    }

    public async Task BuildIndexAsync(IEnumerable<LogFile> logs, IProgress<int> progress, CancellationToken cancellationToken = default)
    {
        _indexedLogs = logs.ToList();

        _writer?.Dispose();
        var cfg = new IndexWriterConfig(_version, _analyzer)
        {
            RAMBufferSizeMB = 256,
            OpenMode = OpenMode.CREATE
        };
        _writer = new IndexWriter(_directory, cfg);

        var total = _indexedLogs.Sum(l => l.Entries.Count);
        var count = 0;

        await Task.Run(() =>
        {
            _indexedLogs
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .WithMergeOptions(ParallelMergeOptions.FullyBuffered)
                .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                .WithCancellation(cancellationToken).
                SelectMany(lf => lf.Entries, (lf, entry) => (lf, entry))
                .ForAll(pair =>
                {
                    var (lf, entry) = pair;
                    var doc = new Document
                    {
                        new StringField("FullPath", lf.Path, Field.Store.YES),
                        new Int32Field("LineNumber", entry.LineNumber, Field.Store.YES),
                        new TextField("ContentRaw", entry.RawLine, Field.Store.YES),
                        new TextField("ContentSubstr", entry.RawLine, Field.Store.YES),
                        new Int64Field("FileTimestamp", lf.CreatedDate.Ticks, Field.Store.YES)
                    };
                    _writer.AddDocument(doc);
                    
                    var c = Interlocked.Increment(ref count);
                    progress.Report(c);
                });

            _writer.Commit();
            _searcher = new IndexSearcher(DirectoryReader.Open(_directory));
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<HitViewModel>> SearchAsync(
        string query,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            if (string.IsNullOrWhiteSpace(query) || _searcher == null)
                return Array.Empty<HitViewModel>();
            
            var termText = query.ToLowerInvariant();
            var termQ    = new TermQuery(new Term(_substrField, termText));

            Query finalQ = termQ;
            if (fromDate.HasValue || toDate.HasValue)
            {
                long min = fromDate?.Ticks ?? long.MinValue;
                long max = toDate?.Ticks   ?? long.MaxValue;
                var range = NumericRangeQuery
                    .NewInt64Range(_timestampField, min, max, true, true);

                var bq = new BooleanQuery
                {
                    { termQ,  Occur.MUST },
                    { range, Occur.MUST }
                };
                finalQ = bq;
            }

            var top = _searcher.Search(finalQ, _indexedLogs.Count * 1000);
            var results = new List<HitViewModel>();

            foreach (var sd in top.ScoreDocs)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var d    = _searcher.Doc(sd.Doc);
                var path = d.Get(_pathField);
                var ln   = d.GetField(_lineField).GetInt32Value() ?? 0;
                var raw  = d.Get(_substrField);

                var lf = _indexedLogs.First(l => l.Path == path);
                var e  = lf.Entries.First(x => x.LineNumber == ln && x.RawLine == raw);

                results.Add(new HitViewModel(lf, e, query, StringComparison.OrdinalIgnoreCase));
            }

            return (IReadOnlyList<HitViewModel>)results;
        }, cancellationToken);
    }

    public void Dispose() => _writer?.Dispose();
}
