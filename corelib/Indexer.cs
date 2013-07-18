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

using InfoStream.Metadata;

namespace InfoStream.Core
{
    /// <summary>
    /// implementazione del sistema di indicizzazione e ricerca
    /// </summary>
    public class ISIndexer : IDisposable
    {
        private string indexerDocumentUniqueIdentifierFieldName = "$FB::ISIDX$UniqueIdentifier$";
        protected delegate void IndexSearcherUpdateHandler(object sender, EventArgs e);

        protected IndexerStorageMode indexMode = IndexerStorageMode.FS;
        protected Analyzer analyzer = null;
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
                            if (!_directoryInfo.Exists || _directoryInfo.GetFiles().Count() < 1)
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
        protected IXQueryCollection EmptySearchResult = new IXQueryCollection() { Count = 0, Start = 0, Take = 0, Results = new IXQuery[]{}, Status = IXQueryStatus.Fail };

        #region public properties
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

        protected string IndexerDocumentUniqueIdentifierFieldName
        {
            get { return indexerDocumentUniqueIdentifierFieldName; }
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
        public ISIndexer(IndexerStorageMode indexMode, IndexerAnalyzer analyzer, Dictionary<string, IndexerAnalyzer> perFieldAnalyzers = null)
            : this(null, indexMode, null, analyzer, perFieldAnalyzers)
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
        public ISIndexer(string indexFullPath, IndexerStorageMode indexMode, IndexerAnalyzer analyzer, Dictionary<string, IndexerAnalyzer> perFieldAnalyzers = null)
            : this(indexFullPath, indexMode, null, analyzer, perFieldAnalyzers)
        {
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="indexFullPath"></param>
        /// <param name="indexMode"></param>
        /// <param name="indexerDocumentUniqueIdentifier"></param>
        /// <param name="analyzer"></param>
        /// <param name="dataItemHandler"></param>
        /// <param name="perFieldAnalyzers"></param>
        public ISIndexer(string indexFullPath, IndexerStorageMode indexMode, string indexerDocumentUniqueIdentifier, IndexerAnalyzer analyzer, Dictionary<string, IndexerAnalyzer> perFieldAnalyzers = null)
        {
            this.indexMode = indexMode;

            if (analyzer != IndexerAnalyzer.PerFieldAnalyzerWrapper)
                this.analyzer = GetAnalyzer(analyzer, false);
            else
            {
                this.analyzer = GetAnalyzer(analyzer, false);
                if (perFieldAnalyzers != null)
                {
                    foreach (KeyValuePair<string, IndexerAnalyzer> kvp in perFieldAnalyzers)
                        (this.analyzer as PerFieldAnalyzerWrapper).AddAnalyzer(kvp.Key, GetAnalyzer(kvp.Value, true));
                }
            }

            if (!String.IsNullOrEmpty(indexFullPath))
                this.luceneIndexFullPath = Path.GetFullPath(indexFullPath);

            if (luceneIndexDirectory == null)
                throw new System.IO.IOException("unable to setup indexing folder");

            if (!String.IsNullOrEmpty(indexerDocumentUniqueIdentifier))
                this.indexerDocumentUniqueIdentifierFieldName = indexerDocumentUniqueIdentifier;

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
        public IXQueryCollection GetAllIndexRecords(IEnumerable<string> fields)
        {
            List<Document> docs = new List<Document>();

            // set up searcher
            TermDocs term = indexSearcher.IndexReader.TermDocs();

            while (term.Next())
                docs.Add(indexSearcher.Doc(term.Doc));

            return new IXQueryCollection
            {
                Results = MapLuceneIndexToDataList(docs, fields),
                Start = 0,
                Take = -1,
                Count = docs.Count,
                Status = (docs.Count > 0) ? IXQueryStatus.Success : IXQueryStatus.NoData
            };
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
        /// <param name="request"></param>
        /// <returns></returns>
        public IXQueryCollection Search(IXRequest request)
        {
            if (request == null || String.IsNullOrEmpty(request.Query))
                return EmptySearchResult;

            return DoSearch(request);
        }

        /// <summary>
        /// add/update an item to the index
        /// </summary>
        /// <param name="dataItem"></param>
        /// <param name="waitForIndex"></param>
        /// <returns></returns>
        public bool AddUpdateLuceneIndex(IXDescriptor dataItem, bool waitForIndex=false)
        {
            return AddUpdateLuceneIndex(new IXDescriptor[] { dataItem }, waitForIndex);
        }

        /// <summary>
        /// add/update items to the index
        /// </summary>
        /// <param name="dataItems"></param>
        /// <param name="waitForIndex"></param>
        /// <returns></returns>
        public bool AddUpdateLuceneIndex(IEnumerable<IXDescriptor> dataItems, bool waitForIndex=false)
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
                    foreach (IXDescriptor dataItem in dataItems)
                    {
                        // remove older index entry
                        // do not call DeleteFromIndex here otherwise it could clash on lock files
                        IXDescriptor.PropertyInfo identifier = dataItem.UniqueIdentifier;
                        TermQuery searchQuery = new TermQuery(new Term(identifier.Name, identifier.Value));
                        if (searchQuery != null)
                            indexWriter.DeleteDocuments(searchQuery);

                        // add new index entry with lucene fields mapped to db fields
                        Document doc = dataItem.ToDocument(indexerDocumentUniqueIdentifierFieldName);

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

            if (dataItemIdentifier == null || String.IsNullOrEmpty(dataItemIdentifier.ToString()))
                return false;

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
                    //FIXME: BUGGATO: deve utilizzare dataitems.Name ma non abbiamo dataitems!!!!!
                    string safeValue = (dataItemIdentifier is byte[]) ? Convert.ToBase64String((byte[])dataItemIdentifier) : dataItemIdentifier.ToString();
                    TermQuery searchQuery = new TermQuery(new Term(indexerDocumentUniqueIdentifierFieldName, safeValue));
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
            if (indexMode == IndexerStorageMode.RAM)
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
        /// <param name="request"></param>
        /// <returns></returns>
        protected IXQueryCollection DoSearch(IXRequest request)
        {
            // validation
            if (String.IsNullOrEmpty(request.Query.Replace("*", "").Replace("?", "")))
                return new IXQueryCollection() { Count = 0, Start = 0, Take = 0, Results = new IXQuery[] { }, Status = IXQueryStatus.ErrorQuerySyntax };

            QueryParser parser = null;

            if (request.Fields.Count() < 1)
                parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_30, null, analyzer);
            else if (request.Fields.Count() == 1)
                parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_30, request.Fields.ElementAt(0), analyzer);
            else
                parser = new MultiFieldQueryParser(Lucene.Net.Util.Version.LUCENE_30, request.Fields.ToArray(), analyzer);

            Query query = ParseQuery(request.Query, parser);

            if (query == null)
                return new IXQueryCollection() { Count = 0, Start = 0, Take = 0, Results = new IXQuery[] { }, Status = IXQueryStatus.ErrorQuerySyntax };;

            bool useScoring = ((request.Flags & IXRequestFlags.UseScore) == IXRequestFlags.UseScore);
            if (useScoring)
                indexSearcher.SetDefaultFieldSortScoring(true, true);
            else
                indexSearcher.SetDefaultFieldSortScoring(false, false);

            TopFieldDocs hits = indexSearcher.Search(query, null, this.MaxSearchHits, Sort.RELEVANCE);

            int skip = request.Skip;
            int take = request.Take;

            if (skip < 0)
                skip = 0;

            if (take < 1)
                take = this.MaxSearchHits;

            return new IXQueryCollection
                            {
                                Results = MapLuceneIndexToDataList(hits.ScoreDocs.Skip(skip).Take(take), indexSearcher, useScoring, request.Fields), 
                                Start = skip, 
                                Take = take, 
                                Count = hits.TotalHits, 
                                Status = (hits.TotalHits>0) ? IXQueryStatus.Success : IXQueryStatus.NoData
                            };
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
        /// <param name="selectedFields"></param>
        /// <returns></returns>
        protected IEnumerable<IXQuery> MapLuceneIndexToDataList(IEnumerable<Document> hits, IEnumerable<string> selectedFields = null)
        {
            return hits.Select(p => p.ToIXQuery(indexerDocumentUniqueIdentifierFieldName, selectedFields)).ToList();
        }

        /// <summary>
        /// map Lucene search index to data
        /// </summary>
        /// <param name="hits"></param>
        /// <param name="searcher"></param>
        /// <param name="useScoring"></param>
        /// <param name="selectedFields"></param>
        /// <returns></returns>
        protected IEnumerable<IXQuery> MapLuceneIndexToDataList(IEnumerable<ScoreDoc> hits, IndexSearcher searcher, bool useScoring, IEnumerable<string> selectedFields = null)
        {
            return hits.Select(p => searcher.Doc(p.Doc).ToIXQuery(indexerDocumentUniqueIdentifierFieldName, selectedFields, ((useScoring) ? (float?)p.Score : null))).ToList();
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
            if (mode == IndexerStorageMode.RAM || di.Exists == false || di.GetFiles().Count()<1)
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
        ~ISIndexer()
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
}