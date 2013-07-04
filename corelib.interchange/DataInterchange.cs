//
// Lucene-based generic indexing and search system
// (C) 2013 Fusionblue
// ===CONFIDENTIAL===
//

// use V1 or V2
#define V1

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace corelib.Interchange
{
    public enum FieldStore
    {
        YES,
        NO
    }

    public enum FieldIndex
    {
        NO,
        ANALYZED,
        NOT_ANALYZED,
        NOT_ANALYZED_NO_NORMS,
        ANALYZED_NO_NORMS
    }

    /// <summary>
    /// classe di informazioni su di un documento indicizzato dall'indexer
    /// viene utilizzato nello scambio dati tra InderInterop e IndexableObjectHandler per permettere l'isolamento di Lucene
    /// </summary>
#if V2
    public class InterchangeDocument
    {
        //private List<InterchangeDocumentFieldInfo> properties = new List<InterchangeDocumentFieldInfo>();
        private Document doc = null;

        internal InterchangeDocument(Lucene.Net.Documents.Document doc)
        {
            this.doc = doc;
        }

        public string Get(string key)
        {
            return doc.Get(key);
        }

        public byte[] GetBinary(string key)
        {
            return doc.GetBinaryValue(key);
        }
    }
#elif V1
    [Serializable]
    public class InterchangeDocument
    {
        private Guid _UniqueIdentifier = Guid.Empty;
        public Guid UniqueIdentifier
        {
            get { return _UniqueIdentifier; }
            set { _UniqueIdentifier = value; }
        }

        private List<InterchangeDocumentFieldInfo> properties = new List<InterchangeDocumentFieldInfo>();
        public List<InterchangeDocumentFieldInfo> Properties
        {
            get { return properties; }
            set { properties = value; }
        }

        public InterchangeDocument()
        {
            _UniqueIdentifier = Guid.NewGuid();
        }

        public InterchangeDocument(IEnumerable<InterchangeDocumentFieldInfo> fields)
        {
            if (fields == null)
                throw new ArgumentNullException("fields");

            _UniqueIdentifier = Guid.NewGuid();
            properties.AddRange(fields);
        }

        public string Get(string key)
        {
            InterchangeDocumentFieldInfo f = properties.SingleOrDefault(p => p.Name == key);
            return (f == null) ? null : f.StringValue;
        }

        public byte[] GetBinary(string key)
        {
            InterchangeDocumentFieldInfo f = properties.SingleOrDefault(p => p.Name == key);
            return (f == null) ? null : f.BinaryValue;
        }
    }
#endif

    [Serializable]
    public class InterchangeDocumentFieldInfo : IFieldInfo, IFieldInfoIndexerSpecific
    {
        public string Name { get; set; }
        public string StringValue { get; set; }
        public byte[] BinaryValue { get; set; }
        public bool IsBinary
        {
            get { return (BinaryValue == null) ? false : true; }
        }

        public FieldStore Store { get; set; }
        public FieldIndex Index { get; set; }

        public InterchangeDocumentFieldInfo()
        {
        }

        public InterchangeDocumentFieldInfo(string name, string value, byte[]binaryValue, FieldStore store, FieldIndex index)
        {
            this.Name = name;
            this.StringValue = (binaryValue == null) ? value : null;
            this.BinaryValue = (binaryValue == null) ? null : binaryValue;
            this.Store = store;
            this.Index = index;
        }
    }

    public interface IFieldInfo
    {
        string Name { get; set; }
        string StringValue { get; set; }
        byte[] BinaryValue { get; set; }
        bool IsBinary { get; }
    }

    public interface IFieldInfoIndexerSpecific
    {
        FieldStore Store { get; set; }
        FieldIndex Index { get; set; }
    }

    /// <summary>
    /// implementazione SPECIFICA del IOH
    /// </summary>
    public class InterchangeDocumentIOH : IndexableObjectHandler<InterchangeDocument>
    {
        public InterchangeDocumentIOH()
        {
        }

        public override string DataItemUniqueIdentifierField
        {
            get { return "IOH$UniqueIdentifier"; }
        }

        public override InterchangeDocument BuildDataItem(InterchangeDocument doc)
        {
            return doc;
        }

        public override InterchangeDocument DocumentParseFromDataItem(InterchangeDocument dataItem)
        {
            return dataItem;
        }

        public override object DataItemUniqueIdentifierValue(InterchangeDocument dataItem)
        {
            return dataItem.UniqueIdentifier;
        }
    }
}
