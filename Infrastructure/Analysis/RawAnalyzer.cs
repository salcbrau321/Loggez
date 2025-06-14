using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Util;

namespace Loggez.Infrastructure.Analysis;

public class RawAnalyzer : Analyzer
{
    private readonly LuceneVersion _version;
    public RawAnalyzer(LuceneVersion version) => _version = version;

    protected override TokenStreamComponents CreateComponents(
        string fieldName, TextReader reader)
    {
        var tokenizer = new KeywordTokenizer(reader);
        var lower     = new LowerCaseFilter(_version, tokenizer);
        return new TokenStreamComponents(tokenizer, lower);
    }
}