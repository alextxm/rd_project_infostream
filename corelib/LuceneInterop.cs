//
// Lucene-based generic indexing and search system
// (C) 2013 Fusionblue
// ===CONFIDENTIAL===
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;

using corelib.Interchange;

namespace corelib
{
    /// <summary>
    /// implementazione del sistema di indicizzazione e ricerca
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class IndexerInterop<T> : IDisposable where T : class
    {
        protected delegate void IndexSearcherUpdateHandler(object sender, EventArgs e);

        protected Type tType = typeof(T);
        protected IndexerStorageMode indexMode = IndexerStorageMode.FS;
        protected Analyzer analyzer = null;
        protected IndexableObjectHandler<T> dataItemHandler = null;
        protected IndexSearcher indexSearcher = null;

        protected string luceneIndexFullPath = Path.Combine("index.lif"); // lucene index file
        protected DirectoryInfo _directoryInfo = null;
        protected Lucene.Net.Store.Directory _directoryTemp = null;
        protected Lucene.Net.Store.Directory luceneIndexDirectory
        {
            get
            {
                if (_directoryTemp == null)
                {
                    try
                    {
                        if (indexMode != IndexerStorageMode.RAM)
                            _directoryInfo = new DirectoryInfo(luceneIndexFullPath);

                        if (indexMode == IndexerStorageMode.FS)
                        {
                            _directoryTemp = FSDirectory.Open(_directoryInfo);
                            DoFSIndexCreation(indexMode, _directoryInfo, _directoryTemp, analyzer);
                        }
                        else if (indexMode == IndexerStorageMode.FSRAM)
                        {
                            // if the index does not exists operate like it was a full RAM-based index
                            if (!_directoryInfo.Exists)
                            {
                                _directoryTemp = new RAMDirectory();
                                DoFSIndexCreation(indexMode, _directoryInfo, _directoryTemp, analyzer); // do THIS BEFORE opening the RAMDirectory otherwise we'll get a crash
                            }
                            else
                            {
                                Lucene.Net.Store.Directory _fsdir = FSDirectory.Open(_directoryInfo);
                                DoFSIndexCreation(indexMode, _directoryInfo, _fsdir, analyzer); // do THIS BEFORE opening the RAMDirectory otherwise we'll get a crash
                                _directoryTemp = new RAMDirectory(_fsdir);
                            }
                        }
                        else
                        {
                            _directoryTemp = new RAMDirectory();
                            DoFSIndexCreation(indexMode, _directoryInfo, _directoryTemp, analyzer); // do THIS BEFORE opening the RAMDirectory otherwise we'll get a crash
                        }

                        // update the searcher, since we've called DoFSIndexCreation()
                        if (indexSearcher != null)
                            this.OnIndexSearcherUpdateRequested(this, new EventArgs());
                    }
                    catch(Exception)
                    {
                        return null;
                    }
                }

                try
                {
                    if (IndexWriter.IsLocked(_directoryTemp))
                        IndexWriter.Unlock(_directoryTemp);

                    if (indexMode != IndexerStorageMode.RAM)
                    {
                        string lockFilePath = Path.Combine(luceneIndexFullPath, "write.lock");

                        if (File.Exists(lockFilePath))
                            File.Delete(lockFilePath);
                    }
                }
                catch
                {
                    _directoryTemp = null;
                }

                return _directoryTemp;
            }
        }

        protected event IndexSearcherUpdateHandler OnIndexSearcherUpdateRequested = null;
        protected IndexerSearchResults<T> EmptySearchResult = new IndexerSearchResults<T>() { Count = 0, Skip = 0, Take = 0, ScoreDocs = new IndexerSearchResultData<T>[]{} };

        #region public properties
        public bool UseScoring { get; set; }
        public int MaxSearchHits { get; set; }

        /// <summary>
        /// get the number of items already in the index
        /// </summary>
        public int IndexSize
        {
            get
            {
                int count = -1;

                try
                {
                    using (IndexReader reader = IndexReader.Open(luceneIndexDirectory, false))
                    {
                        count = reader.NumDocs();
                    }
                }
                catch (Exception)
                {
                }

                return count;
            }
        }
        #endregion

        #region constructors
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="indexMode"></param>
        /// <param name="analyzer"></param>
        /// <param name="dataItemHandler"></param>
        /// <param name="perFieldAnalyzers"></param>
        public IndexerInterop(IndexerStorageMode indexMode, IndexerAnalyzer analyzer, IndexableObjectHandler<T> dataItemHandler, Dictionary<string, IndexerAnalyzer> perFieldAnalyzers = null)
            : this(null, indexMode, analyzer, dataItemHandler, perFieldAnalyzers)
        {
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="indexFullPath"></param>
        /// <param name="indexMode"></param>
        /// <param name="analyzer"></param>
        /// <param name="dataItemHandler"></param>
        /// <param name="perFieldAnalyzers"></param>
        public IndexerInterop(string indexFullPath, IndexerStorageMode indexMode, IndexerAnalyzer analyzer, IndexableObjectHandler<T> dataItemHandler, Dictionary<string, IndexerAnalyzer> perFieldAnalyzers = null)
        {
            if (dataItemHandler == null)
                throw new ArgumentNullException("dataItemHandler");

            this.indexMode = indexMode;
            this.dataItemHandler = dataItemHandler;

            if(analyzer != IndexerAnalyzer.PerFieldAnalyzerWrapper)
                this.analyzer = GetAnalyzer(analyzer, false);
            else
            {
                this.analyzer = GetAnalyzer(analyzer, false);
                if(perFieldAnalyzers!=null)
                {
                    foreach(KeyValuePair<string,IndexerAnalyzer> kvp in perFieldAnalyzers)
                        (this.analyzer as PerFieldAnalyzerWrapper).AddAnalyzer(kvp.Key, GetAnalyzer(kvp.Value, true));
                }
            }

            if (!String.IsNullOrEmpty(indexFullPath))
                this.luceneIndexFullPath = Path.GetFullPath(indexFullPath);

            if (luceneIndexDirectory == null)
                throw new System.IO.IOException("unable to setup indexing folder");

            this.indexSearcher = new IndexSearcher(luceneIndexDirectory, true);
            this.OnIndexSearcherUpdateRequested += IndexSearcherUpdater;
            this.MaxSearchHits = 1000; // DEFAULT
        }
        #endregion

        #region public methods
        /// <summary>
        /// get all indexed records
        /// </summary>
        /// <returns></returns>
        public IEnumerable<T> GetAllIndexRecords()
        {
            List<Document> docs = new List<Document>();

            // set up lucene searcher
            TermDocs term = indexSearcher.IndexReader.TermDocs();

            while (term.Next())
                docs.Add(indexSearcher.Doc(term.Doc));

            return MapLuceneIndexToDataList(docs);
        }

        /// <summary>
        /// get a list of all the available fields being indexed
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetIndexedFields()
        {
            return indexSearcher.IndexReader.GetFieldNames(IndexReader.FieldOption.INDEXED).AsEnumerable();
        }

        /// <summary>
        /// search for a specific record
        /// </summary>
        /// <param name="input"></param>
        /// <param name="fieldName"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <param name="selectedFields"></param>
        /// <returns></returns>
        public IndexerSearchResults<T> Search(string input, string fieldName = null, int skip = 0, int take = 0, IEnumerable<string> selectedFields = null)
        {
            if (String.IsNullOrEmpty(input))
                return EmptySearchResult;

            //IEnumerable<string> terms = input.Trim().Replace("-", " ").Split(' ').Where(x => !String.IsNullOrEmpty(x)).Select(x => x.Trim() + "*");
            IEnumerable<string> terms = input.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Where(x => !String.IsNullOrEmpty(x)).Select(x => x.Trim());
            input = String.Join(" ", terms);

            return DoSearch(input, new string[] { fieldName }, skip, take);
        }

        /// <summary>
        /// search for a specific record
        /// </summary>
        /// <param name="input"></param>
        /// <param name="fieldName"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <param name="selectedFields"></param>
        /// <returns></returns>
        public IndexerSearchResults<T> SearchDefault(string input, string fieldName = null, int skip = 0, int take = 0, IEnumerable<string> selectedFields = null)
        {
            return String.IsNullOrEmpty(input) ? EmptySearchResult : DoSearch(input, new string[] { fieldName }, skip, take);
        }

        /// <summary>
        /// search for a specific record
        /// </summary>
        /// <param name="input"></param>
        /// <param name="fieldNames"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <param name="selectedFields"></param>
        /// <returns></returns>
        public IndexerSearchResults<T> Search(string input, IEnumerable<string> fieldNames, int skip = 0, int take = 0, IEnumerable<string> selectedFields = null)
        {
            if (String.IsNullOrEmpty(input))
                return EmptySearchResult;

            //IEnumerable<string> terms = input.Trim().Replace("-", " ").Split(' ').Where(x => !String.IsNullOrEmpty(x)).Select(x => x.Trim() + "*");
            IEnumerable<string> terms = input.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Where(x => !String.IsNullOrEmpty(x)).Select(x => x.Trim());
            input = String.Join(" ", terms);

            return DoSearch(input, fieldNames, skip, take);
        }

        /// <summary>
        /// search for a specific record
        /// </summary>
        /// <param name="input"></param>
        /// <param name="fieldNamse"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <param name="selectedFields"></param>
        /// <returns></returns>
        public IndexerSearchResults<T> SearchDefault(string input, IEnumerable<string> fieldNames, int skip = 0, int take = 0, IEnumerable<string> selectedFields = null)
        {
            return String.IsNullOrEmpty(input) ? EmptySearchResult : DoSearch(input, fieldNames, skip, take);
        }

        /// <summary>
        /// search for a specific record
        /// </summary>
        /// <param name="input"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <param name="selectedFields"></param>
        /// <returns></returns>
        public IndexerSearchResults<T> SearchDefault(string input, int skip = 0, int take = 0, IEnumerable<string> selectedFields = null)
        {
            return String.IsNullOrEmpty(input) ? EmptySearchResult : DoSearch(input, null, skip, take);
        }

        /// <summary>
        /// add/update an item to the index
        /// </summary>
        /// <param name="dataItem"></param>
        /// <param name="waitForIndex"></param>
        /// <returns></returns>
        public bool AddUpdateLuceneIndex(T dataItem, bool waitForIndex=false)
        {
            return AddUpdateLuceneIndex(new T[] { dataItem }, waitForIndex);
        }

        /// <summary>
        /// add/update items to the index
        /// </summary>
        /// <param name="dataItems"></param>
        /// <param name="waitForIndex"></param>
        /// <returns></returns>
        public bool AddUpdateLuceneIndex(IEnumerable<T> dataItems, bool waitForIndex=false)
        {
            if (waitForIndex)
                while (IndexWriter.IsLocked(luceneIndexDirectory)) { }
            else
            {
                if (IndexWriter.IsLocked(luceneIndexDirectory))
                    return false;
            }

            try
            {
                using (IndexWriter indexWriter = new IndexWriter(luceneIndexDirectory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
                {
                    // add data to lucene search index (replaces older entries if any)
                    foreach (T dataItem in dataItems)
                    {
                        // remove older index entry
                        // do not call DeleteFromIndex here otherwise it could clash on lock files
                        TermQuery searchQuery = new TermQuery(new Term(dataItemHandler.DataItemUniqueIdentifierField, dataItemHandler.DataItemUniqueIdentifierValue(dataItem).ToString()));
                        if (searchQuery != null)
                            indexWriter.DeleteDocuments(searchQuery);

                        // add new index entry with lucene fields mapped to db fields
                        Document doc = (tType==typeof(InterchangeDocument)) ? (dataItem as InterchangeDocument).ToDocument() : dataItemHandler.DocumentParseFromDataItem(dataItem).ToDocument();

                        // add lucene fields mapped to db fields
                        //foreach (InterchangeDocumentFieldInfo f in dataItemHandler.FieldsParseFromDataItem(dataItem))
                        //    doc.Add(f.ToField());

                        // add entry to index
                        indexWriter.AddDocument(doc);
                    }
                }

                // update the searcher
                this.OnIndexSearcherUpdateRequested(this, new EventArgs());
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// clear the specified record from the index
        /// </summary>
        /// <param name="waitForIndex"></param>
        /// <returns></returns>
        public bool DeleteFromIndex(object dataItemIdentifier, bool waitForIndex=false)
        {
            bool ret = false;

            if (waitForIndex)
                while (IndexWriter.IsLocked(luceneIndexDirectory)) { }
            else
            {
                if (IndexWriter.IsLocked(luceneIndexDirectory))
                    return false;
            }

            try
            {
                using (IndexWriter indexWriter = new IndexWriter(luceneIndexDirectory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
                {
                    // remove older index entry
                    TermQuery searchQuery = new TermQuery(new Term(dataItemHandler.DataItemUniqueIdentifierField, dataItemIdentifier.ToString()));
                    if (searchQuery != null)
                        indexWriter.DeleteDocuments(searchQuery);
                }

                // update the searcher
                this.OnIndexSearcherUpdateRequested(this, new EventArgs());

                ret = true;
            }
            catch (Exception)
            {
                ret = false;
            }

            return ret;
        }

        /// <summary>
        /// clears the whole index (delete all indexes items)
        /// </summary>
        /// <param name="waitForIndex"></param>
        /// <returns></returns>
        public bool ClearAllIndex(bool waitForIndex=false)
        {
            if (waitForIndex)
                while (IndexWriter.IsLocked(luceneIndexDirectory)) { }
            else
            {
                if (IndexWriter.IsLocked(luceneIndexDirectory))
                    return false;
            }

            try
            {
                using (IndexWriter indexWriter = new IndexWriter(luceneIndexDirectory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
                {
                    // remove older index entries
                    indexWriter.DeleteAll();
                }

                // update the searcher
                this.OnIndexSearcherUpdateRequested(this, new EventArgs());
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// optimize index
        /// </summary>
        /// <param name="waitForIndex"></param>
        /// <returns></returns>
        public bool OptimizeIndex(bool waitForIndex=false)
        {
            if (waitForIndex)
                while (IndexWriter.IsLocked(luceneIndexDirectory)) { }
            else
            {
                if (IndexWriter.IsLocked(luceneIndexDirectory))
                    return false;
            }

            try
            {
                using (IndexWriter indexWriter = new IndexWriter(luceneIndexDirectory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
                {
                    indexWriter.Optimize();
                }

                // update the searcher
                this.OnIndexSearcherUpdateRequested(this, new EventArgs());

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool UpdateMemoryIndexFromFS()
        {
            if (indexMode != IndexerStorageMode.FSRAM)
                return false;

            try
            {
                _directoryTemp = null;

                if (IndexWriter.IsLocked(luceneIndexDirectory))
                    return false;

                this.OnIndexSearcherUpdateRequested(this, new EventArgs());
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        #endregion

        #region private methods
        /// <summary>
        /// core search method
        /// </summary>
        /// <param name="searchQuery"></param>
        /// <param name="searchFields"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        protected IndexerSearchResults<T> DoSearch(string searchQuery, IEnumerable<string> searchFields, int skip, int take)
        {
            // validation
            if (String.IsNullOrEmpty(searchQuery.Replace("*", "").Replace("?", "")))
                return EmptySearchResult;

            //if (searchFields == null)
            //    return EmptySearchResult;

            QueryParser parser = null;

            if (searchFields.Count() < 1)
                parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_30, null, analyzer);
            else if (searchFields.Count() == 1)
                parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_30, searchFields.ElementAt(0), analyzer);
            else
                parser = new MultiFieldQueryParser(Lucene.Net.Util.Version.LUCENE_30, searchFields.ToArray(), analyzer);

            Query query = ParseQuery(searchQuery, parser);

            if (query == null)
                return EmptySearchResult;

            if(UseScoring)
                indexSearcher.SetDefaultFieldSortScoring(true, true);
            else
                indexSearcher.SetDefaultFieldSortScoring(false, false);

            TopFieldDocs hits = indexSearcher.Search(query, null, this.MaxSearchHits, Sort.RELEVANCE);

            if (skip < 0)
                skip = 0;

            if (take < 1)
                take = this.MaxSearchHits;

            IndexerSearchResults<T> results = new IndexerSearchResults<T>() { ScoreDocs = MapLuceneIndexToDataList(hits.ScoreDocs.Skip(skip).Take(take), indexSearcher), Skip = skip, Take = take, Count = hits.TotalHits };
            //results = MapLuceneIndexToDataList(hits, indexSearcher);

            return results;
        }

        /// <summary>
        /// parse a search query
        /// </summary>
        /// <param name="searchQuery"></param>
        /// <param name="parser"></param>
        /// <returns></returns>
        protected Query ParseQuery(string searchQuery, QueryParser parser)
        {
            Query query = null;
            bool end = false;
            int pass = 0;

            while (!end)
            {
                if (pass > 2 || query != null)
                {
                    end = true;
                    continue;
                }

                try
                {
                    //Console.WriteLine("BEFORE1: {0}  AFTER: {1}", searchQuery.Trim(), QueryParser.Escape(searchQuery.Trim()));
                    if (pass < 1)
                        query = parser.Parse(searchQuery.Trim());
                    else
                        query = parser.Parse(QueryParser.Escape(searchQuery.Trim()));
                }
                catch (Exception)
                {
                    ++pass;
                    query = null;
                }
            }

            return query;
        }

        /// <summary>
        /// map Lucene search index to data
        /// </summary>
        /// <param name="hits"></param>
        /// <returns></returns>
        protected IEnumerable<T> MapLuceneIndexToDataList(IEnumerable<Document> hits, IEnumerable<string> selectedFields = null)
        {
            // Utils.CreateInstance<InterchangeDocument, Document>(p)
            return hits.Select(p => dataItemHandler.BuildDataItem(p.ToInterchangeDocument(dataItemHandler, selectedFields))).ToList();
            //return hits.Select(dataItemHandler.BuildDataItem).ToList();
        }

        /// <summary>
        /// map Lucene search index to data
        /// </summary>
        /// <param name="hits"></param>
        /// <param name="searcher"></param>
        /// <param name="selectedFields"></param>
        /// <returns></returns>
        protected IEnumerable<IndexerSearchResultData<T>> MapLuceneIndexToDataList(IEnumerable<ScoreDoc> hits, IndexSearcher searcher, IEnumerable<string> selectedFields = null)
        {
            //Utils.CreateInstance<InterchangeDocument, Document>(searcher.Doc(hit.Doc))
            return hits.Select(hit => new IndexerSearchResultData<T>() { Score = hit.Score, Element = dataItemHandler.BuildDataItem(searcher.Doc(hit.Doc).ToInterchangeDocument(dataItemHandler, selectedFields)) }).ToList();
            //return hits.Select(hit => dataItemHandler.BuildDataItem(searcher.Doc(hit.Doc))).ToList();
        }

        /// <summary>
        /// handler for the OnIndexSearcherUpdateRequested event: the global indexsearcher must be recreated after the index had been modified to reflect changes to the endusers
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void IndexSearcherUpdater(object sender, EventArgs e)
        {
            lock(indexSearcher)
            {
                indexSearcher.Dispose();
                indexSearcher = new IndexSearcher(luceneIndexDirectory, true);
            }
        }

        /// <summary>
        /// create index if needed
        /// </summary>
        protected void DoFSIndexCreation(IndexerStorageMode mode, DirectoryInfo di, Lucene.Net.Store.Directory directoryTemp, Analyzer analyzer)
        {
            if (mode == IndexerStorageMode.RAM || di.Exists == false)
            {
                using (IndexWriter indexWriter = new IndexWriter(directoryTemp, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
                {
                    indexWriter.Commit(); // create segments
                }
            }
        }

        /// <summary>
        /// get the specified analyzer
        /// </summary>
        /// <param name="indexerAnalyzer"></param>
        /// <param name="isSub"></param>
        /// <returns></returns>
        protected Analyzer GetAnalyzer(IndexerAnalyzer indexerAnalyzer, bool isSub)
        {
            Analyzer analyzer = null;

            switch (indexerAnalyzer)
            {
                case IndexerAnalyzer.KeywordAnalyzer:
                    analyzer = new KeywordAnalyzer();
                    break;

                case IndexerAnalyzer.PerFieldAnalyzerWrapper:
                    if(!isSub)
                        analyzer = new PerFieldAnalyzerWrapper(new Lucene.Net.Analysis.Standard.StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30));
                    else
                        analyzer = new Lucene.Net.Analysis.Standard.StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);
                    break;

                case IndexerAnalyzer.SimpleAnalyzer:
                    analyzer = new SimpleAnalyzer();
                    break;

                case IndexerAnalyzer.StopAnalyzer:
                    analyzer = new StopAnalyzer(Lucene.Net.Util.Version.LUCENE_30);
                    break;

                case IndexerAnalyzer.WhitespaceAnalyzer:
                    analyzer = new WhitespaceAnalyzer();
                    break;

                case IndexerAnalyzer.StandardAnalyzer:
                default:
                    analyzer = new Lucene.Net.Analysis.Standard.StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);
                    break;
            }

            if (isSub)
            {

            }

            return analyzer;
        }
        #endregion

        #region IDisposable implementation
        ~IndexerInterop()
        {
            Dispose(false);
        }

        private bool disposed = false;

        //Implement IDisposable.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (analyzer != null)
                    {
                        analyzer.Dispose();
                        analyzer = null;
                    }

                    if (indexSearcher != null)
                    {
                        indexSearcher.Dispose();
                        indexSearcher = null;
                    }
                }

                // Free your own state (unmanaged objects).
                // Set large fields to null.
                disposed = true;
            }
        }
        #endregion
    }

    public class IndexerSearchResultData<T>
    {
        public float Score { get; set; }
        public T Element { get; set; }
    }

    public class IndexerSearchResults<T>
    {
        public int Count { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
        public IEnumerable<IndexerSearchResultData<T>> ScoreDocs { get; set; }
    }
}