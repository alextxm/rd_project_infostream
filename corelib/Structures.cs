using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfoStream.Metadata;

namespace InfoStream.Core
{
    public enum IndexerStorageMode
    {
        FS,
        FSRAM,
        RAM
    }

    public enum IndexerAnalyzer
    {
        KeywordAnalyzer,
        PerFieldAnalyzerWrapper,
        SimpleAnalyzer,
        StandardAnalyzer,
        StopAnalyzer,
        WhitespaceAnalyzer
    }
}
