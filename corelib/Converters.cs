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
using System.Reflection.Emit;

using Blackbird.Core.Runtime;
using Lucene.Net.Documents;

using corelib.Interchange;

namespace corelib
{
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

        public static Field ToField(this InterchangeDocumentFieldInfo idfield)
        {
            if (idfield.IsBinary)
                return new Field(idfield.Name, idfield.BinaryValue, 0, idfield.BinaryValue.Length, idfield.Store.ToFieldStore());
            else
                return new Field(idfield.Name, idfield.StringValue, idfield.Store.ToFieldStore(), idfield.Index.ToFieldIndex());
        }

        public static FieldIndex ToFieldIndex(this IFieldable fieldable)
        {
            if (!fieldable.IsIndexed)
                return FieldIndex.NO;

            if (fieldable.IsTokenized)
                return (fieldable.OmitNorms) ? FieldIndex.ANALYZED_NO_NORMS : FieldIndex.ANALYZED;
            else
                return (fieldable.OmitNorms) ? FieldIndex.NOT_ANALYZED_NO_NORMS : FieldIndex.NOT_ANALYZED;
        }

        public static InterchangeDocument ToInterchangeDocument<T>(this Document doc, IndexableObjectHandler<T> ioh, IEnumerable<string> selectedFields)
        {
            InterchangeDocument iDoc = new InterchangeDocument(ioh.DataItemUniqueIdentifierField);
            IQueryable<IFieldable> fields = doc.GetFields().AsQueryable();
            
            if(selectedFields!=null && selectedFields.Count()>0)
                fields = fields.Where(p => selectedFields.Contains(p.Name));
               
            fields.ToList().ForEach(f => iDoc.Properties.Add(new InterchangeDocumentFieldInfo()
                                                                            {
                                                                                Name = f.Name,
                                                                                StringValue = (f.IsBinary) ? String.Empty : f.StringValue,
                                                                                BinaryValue = (f.IsBinary) ? f.GetBinaryValue() : null,
                                                                                Store = f.IsStored ? FieldStore.YES : FieldStore.NO,
                                                                                Index = f.ToFieldIndex()
                                                                            }
                                                                     ));

            return iDoc;
        }

        public static Document ToDocument(this InterchangeDocument idoc)
        {
            Document doc = new Document();
            
            //doc.Add(new Field("IOH$UniqueIdentifier", idoc.UniqueIdentifier.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
            
            foreach (InterchangeDocumentFieldInfo f in idoc.Properties)
                doc.Add(f.ToField());

            return doc;
        }
    }
}
