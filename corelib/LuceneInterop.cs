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
        internal delegate void IndexSearcherUpdateHandler(object sender, EventArgs e);

        private IndexerStorageMode indexMode = IndexerStorageMode.FS;
        private Analyzer analyzer = null;
        private IndexableObjectHandler<T> dataItemHandler = null;
        private IndexSearcher indexSearcher = null;

        // properties
        private string luceneIndexFullPath = Path.Combine("index.lif"); // lucene index file

        private DirectoryInfo _directoryInfo = null;
        private Lucene.Net.Store.Directory _directoryTemp = null;

        private Lucene.Net.Store.Directory luceneIndexDirectory
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
                    catch(Exception ex)
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

        private event IndexSearcherUpdateHandler OnIndexSearcherUpdateRequested = null;

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
                catch (Exception ex)
                {
                }

                return count;
            }
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="indexMode"></param>
        /// <param name="analyzer"></param>
        /// <param name="dataItemHandler"></param>
        public IndexerInterop(IndexerStorageMode indexMode, IndexerAnalyzer analyzer, IndexableObjectHandler<T> dataItemHandler)
        {
            if (dataItemHandler == null)
                throw new ArgumentNullException("dataItemHandler");

            this.indexMode = indexMode;
            this.dataItemHandler = dataItemHandler;

            switch (analyzer)
            {
                case IndexerAnalyzer.KeywordAnalyzer:
                    this.analyzer = new KeywordAnalyzer();
                    break;

                case IndexerAnalyzer.PerFieldAnalyzerWrapper:
                    this.analyzer = new PerFieldAnalyzerWrapper(new Lucene.Net.Analysis.Standard.StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30));
                    break;

                case IndexerAnalyzer.SimpleAnalyzer:
                    this.analyzer = new SimpleAnalyzer();
                    break;

                case IndexerAnalyzer.StopAnalyzer:
                    this.analyzer = new StopAnalyzer(Lucene.Net.Util.Version.LUCENE_30);
                    break;

                case IndexerAnalyzer.WhitespaceAnalyzer:
                    this.analyzer = new WhitespaceAnalyzer();
                    break;

                case IndexerAnalyzer.StandardAnalyzer:
                default:
                    this.analyzer = new Lucene.Net.Analysis.Standard.StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);
                    break;
            }

            if (luceneIndexDirectory == null)
                throw new System.IO.IOException("unable to setup indexing folder");

            this.indexSearcher = new IndexSearcher(luceneIndexDirectory, true);
            this.OnIndexSearcherUpdateRequested += IndexSearcherUpdater;
        }

        // ---------------------------------------------------------------------------------------

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
        /// search for a specific record
        /// </summary>
        /// <param name="input"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public IEnumerable<T> Search(string input, string fieldName = null)
        {
            if (String.IsNullOrEmpty(input))
                return new List<T>();

            //IEnumerable<string> terms = input.Trim().Replace("-", " ").Split(' ').Where(x => !String.IsNullOrEmpty(x)).Select(x => x.Trim() + "*");
            IEnumerable<string> terms = input.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Where(x => !String.IsNullOrEmpty(x)).Select(x => x.Trim());
            input = String.Join(" ", terms);

            return DoSearch(input, new string[] { fieldName });
        }

        /// <summary>
        /// search for a specific record
        /// </summary>
        /// <param name="input"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public IEnumerable<T> SearchDefault(string input, string fieldName = null)
        {
            return String.IsNullOrEmpty(input) ? new T[] { } : DoSearch(input, new string[] { fieldName });
        }

        /// <summary>
        /// search for a specific record
        /// </summary>
        /// <param name="input"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public IEnumerable<T> Search(string input, IEnumerable<string> fieldNames)
        {
            if (String.IsNullOrEmpty(input))
                return new List<T>();

            //IEnumerable<string> terms = input.Trim().Replace("-", " ").Split(' ').Where(x => !String.IsNullOrEmpty(x)).Select(x => x.Trim() + "*");
            IEnumerable<string> terms = input.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Where(x => !String.IsNullOrEmpty(x)).Select(x => x.Trim());
            input = String.Join(" ", terms);

            return DoSearch(input, fieldNames);
        }

        /// <summary>
        /// search for a specific record
        /// </summary>
        /// <param name="input"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public IEnumerable<T> SearchDefault(string input, IEnumerable<string> fieldNames)
        {
            return String.IsNullOrEmpty(input) ? new T[] { } : DoSearch(input, fieldNames);
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
                        Document doc = dataItemHandler.DocumentParseFromDataItem(dataItem).ToDocument();

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
            catch (Exception ex)
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
            catch (Exception ex)
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
            catch (Exception ex)
            {
                return false;
            }
        }

        #region private methods
        /// <summary>
        /// core search method
        /// </summary>
        /// <param name="searchQuery"></param>
        /// <param name="searchFields"></param>
        /// <param name="hitsLimit"></param>
        /// <returns></returns>
        private IEnumerable<T> DoSearch(string searchQuery, IEnumerable<string> searchFields, int hitsLimit = 1000)
        {
            // validation
            if (String.IsNullOrEmpty(searchQuery.Replace("*", "").Replace("?", "")))
                return new T[] { };

            IEnumerable<T> results = null;

            if (searchFields == null)
                return new T[] { };

            QueryParser parser = null;

            if (searchFields.Count() < 1)
                parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_30, searchFields.ElementAt(0), analyzer);
            else
                parser = new MultiFieldQueryParser(Lucene.Net.Util.Version.LUCENE_30, searchFields.ToArray(), analyzer);

            Query query = ParseQuery(searchQuery, parser);
            IEnumerable<ScoreDoc> hits = indexSearcher.Search(query, null, hitsLimit, Sort.RELEVANCE).ScoreDocs;
            results = MapLuceneIndexToDataList(hits, indexSearcher);

            return results;
        }

        /// <summary>
        /// parse a search query
        /// </summary>
        /// <param name="searchQuery"></param>
        /// <param name="parser"></param>
        /// <returns></returns>
        private Query ParseQuery(string searchQuery, QueryParser parser)
        {
            Query query;

            try
            {
                //Console.WriteLine("BEFORE1: {0}  AFTER: {1}", searchQuery.Trim(), QueryParser.Escape(searchQuery.Trim()));
                query = parser.Parse(searchQuery.Trim());
            }
            catch (ParseException)
            {
                //Console.WriteLine("BEFORE2: {0}  AFTER: {1}", searchQuery.Trim(), QueryParser.Escape(searchQuery.Trim()));
                query = parser.Parse(QueryParser.Escape(searchQuery.Trim()));
            }

            return query;
        }

        /// <summary>
        /// map Lucene search index to data
        /// </summary>
        /// <param name="hits"></param>
        /// <returns></returns>
        private IEnumerable<T> MapLuceneIndexToDataList(IEnumerable<Document> hits)
        {
            // Utils.CreateInstance<InterchangeDocument, Document>(p)
            return hits.Select(p => dataItemHandler.BuildDataItem(p.ToInterchangeDocument())).ToList();
            //return hits.Select(dataItemHandler.BuildDataItem).ToList();
        }

        /// <summary>
        /// map Lucene search index to data
        /// </summary>
        /// <param name="hits"></param>
        /// <param name="searcher"></param>
        /// <returns></returns>
        private IEnumerable<T> MapLuceneIndexToDataList(IEnumerable<ScoreDoc> hits, IndexSearcher searcher)
        {
            //Utils.CreateInstance<InterchangeDocument, Document>(searcher.Doc(hit.Doc))
            return hits.Select(hit => dataItemHandler.BuildDataItem(searcher.Doc(hit.Doc).ToInterchangeDocument())).ToList();
            //return hits.Select(hit => dataItemHandler.BuildDataItem(searcher.Doc(hit.Doc))).ToList();
        }

        /// <summary>
        /// handler for the OnIndexSearcherUpdateRequested event: the global indexsearcher must be recreated after the index had been modified to reflect changes to the endusers
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IndexSearcherUpdater(object sender, EventArgs e)
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
        private void DoFSIndexCreation(IndexerStorageMode mode, DirectoryInfo di, Lucene.Net.Store.Directory directoryTemp, Analyzer analyzer)
        {
            if (mode == IndexerStorageMode.RAM || di.Exists == false)
            {
                using (IndexWriter indexWriter = new IndexWriter(directoryTemp, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
                {
                    indexWriter.Commit(); // create segments
                }
            }
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
}