using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.NGram;
using Lucene.Net.Util;

namespace Loggez.Infrastructure.Analysis;

public class SubstringAnalyzer : Analyzer
{
    private readonly LuceneVersion _version;
    public SubstringAnalyzer(LuceneVersion version) => _version = version;

    protected override TokenStreamComponents CreateComponents(
        string fieldName, TextReader reader)
    {
        var tokenizer = new KeywordTokenizer(reader);
        var lower     = new LowerCaseFilter(_version, tokenizer);
        var ngrams    = new NGramTokenFilter(_version, lower, 4, 25);
        return new TokenStreamComponents(tokenizer, ngrams);
    }
}