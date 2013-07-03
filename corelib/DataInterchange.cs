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

using Lucene.Net.Documents;

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
    /// classe di informazioni su di un campo di un documento da indicizzare
    /// viene utilizzato nello scambio dati tra InderInterop e IndexableObjectHandler per permettere l'isolamento di Lucene
    /// </summary>
    public class InterchangeField
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public FieldStore Store { get; set; }
        public FieldIndex Index { get; set; }

        public InterchangeField(string name, string value, FieldStore store, FieldIndex index)
        {
            this.Name = name;
            this.Value = value;
            this.Store = store;
            this.Index = index;
        }
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
    public class InterchangeDocument
    {
        private List<InterchangeDocumentFieldInfo> properties = new List<InterchangeDocumentFieldInfo>();

        public InterchangeDocument()
        {
        }

        internal InterchangeDocument(Lucene.Net.Documents.Document doc)
        {
            foreach (Field f in doc.GetFields())
            {
                properties.Add(new InterchangeDocumentFieldInfo()
                                   {
                                       Name = f.Name,
                                       IsBinary = f.IsBinary,
                                       StringValue = (f.IsBinary) ? String.Empty : f.StringValue,
                                       BinaryValue = (f.IsBinary) ? f.GetBinaryValue() : null
                                   });
            }
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

    internal class InterchangeDocumentFieldInfo
    {
        public string Name { get; set; }
        public string StringValue { get; set; }
        public byte[] BinaryValue { get; set; }
        public bool IsBinary { get; set; }
    }

    /// <summary>
    /// convertitori tra InterchangeXXXXXX e gli equivalenti Lucene
    /// </summary>
    internal static class Extensions
    {
        public static Field.Store ToFieldStore(this FieldStore store)
        {
            return (store == FieldStore.YES) ? Field.Store.YES : Field.Store.NO;
        }

        public static Field.Index ToFieldIndex(this FieldIndex index)
        {
            switch (index)
            {
                case FieldIndex.NOT_ANALYZED:
                    return Field.Index.NOT_ANALYZED;

                case FieldIndex.NOT_ANALYZED_NO_NORMS:
                    return Field.Index.NOT_ANALYZED_NO_NORMS;

                case FieldIndex.ANALYZED:
                    return Field.Index.ANALYZED;

                case FieldIndex.ANALYZED_NO_NORMS:
                    return Field.Index.ANALYZED_NO_NORMS;

                case FieldIndex.NO:
                default:
                    return Field.Index.NO;
            }
        }

        public static Field ToField(this InterchangeField ifield)
        {
            return new Field(ifield.Name, ifield.Value, ifield.Store.ToFieldStore(), ifield.Index.ToFieldIndex()); 
        }
    }
}
