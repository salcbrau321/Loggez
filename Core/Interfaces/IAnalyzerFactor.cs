using Lucene.Net.Analysis;

namespace Loggez.Core.Interfaces;

public interface IAnalyzerFactory
{
    Analyzer Create(string fieldName);
}