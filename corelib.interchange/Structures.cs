using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using corelib.Interchange;

namespace corelib
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

#if DEPRECATED
    /// <summary>
    /// classe che definisce le operazioni specifiche sul tipo da indicizzare
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class IndexableObjectHandler<T>
    {
        public abstract InterchangeDocument DocumentParseFromDataItem(T dataItem);
        public abstract T BuildDataItem(InterchangeDocument doc);

        public abstract string DataItemUniqueIdentifierField { get; }
        public abstract object DataItemUniqueIdentifierValue(T dataItem);
    }
#endif
}
