using System.Collections.Generic;
using Loggez.Core.Interfaces;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Util;

namespace Loggez.Infrastructure.Analysis;

public class AnalyzerFactory : IAnalyzerFactory
{
    private readonly Analyzer _perField;
        
    public AnalyzerFactory()
    {
        var substr = new SubstringAnalyzer(LuceneVersion.LUCENE_48);
        var raw    = new RawAnalyzer(LuceneVersion.LUCENE_48);

        _perField = new PerFieldAnalyzerWrapper(
            substr,
            new Dictionary<string, Analyzer>
            {
                { "ContentRaw", raw }
            });
    }

    public Analyzer Create(string fieldName)
    {
        return _perField;
    }
}