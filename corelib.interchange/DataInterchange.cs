//
// Lucene-based generic indexing and search system
// (C) 2013 Fusionblue
// ===CONFIDENTIAL===
//

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

    public class InterchangeDocumentsCollection
    {
        public int Count { get; set; }
        public int Start { get; set; }
        public int Take { get; set; }
        public bool Result { get; set; }
        public IEnumerable<InterchangeDocumentInfo> InterchangeDocuments { get; set; }
    }

    public class InterchangeDocumentInfo
    {
        public float Score { get; set; }
        public InterchangeDocument Element { get; set; }
    }

    /// <summary>
    /// classe di informazioni su di un documento indicizzato dall'indexer
    /// viene utilizzato nello scambio dati tra InderInterop e IndexableObjectHandler per permettere l'isolamento di Lucene
    /// </summary>
    [Serializable]
    public class InterchangeDocument
    {
        private string uniqueIdentifierField = null;
        public string UniqueIdentifierField
        {
            get { return uniqueIdentifierField; }
            set { uniqueIdentifierField = value; }
        }

        private List<InterchangeDocumentFieldInfo> properties = new List<InterchangeDocumentFieldInfo>();
        public List<InterchangeDocumentFieldInfo> Properties
        {
            get { return properties; }
            set { properties = value; }
        }

        public InterchangeDocument(string uniqueIdentifierField)
        {
            if (String.IsNullOrEmpty(uniqueIdentifierField))
                throw new ArgumentNullException("uniqueIdentifierField");
        }

        public InterchangeDocument(string uniqueIdentifierField, IEnumerable<InterchangeDocumentFieldInfo> fields)
        {
            if (fields == null)
                throw new ArgumentNullException("fields");

            if(String.IsNullOrEmpty(uniqueIdentifierField) || !fields.Any(p=> p.Name == uniqueIdentifierField))
                throw new ArgumentNullException("uniqueIdentifierField");

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

        public InterchangeDocumentFieldInfo(string name, string value, byte[] binaryValue, FieldStore store, FieldIndex index)
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
            return dataItem.Get(dataItem.UniqueIdentifierField);
        }
    }
}
