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

using Lucene.Net.Documents;

using InfoStream.Metadata;

namespace InfoStream.Core
{
    /// <summary>
    /// convertitori tra IX* e gli equivalenti Lucene
    /// </summary>
    internal static class IndexerExtensions
    {
        internal static readonly string DocumentUniqueIdentifierFieldName
        {
            get { return "$FB::ISIDX$UniqueIdentifier$"; }
        }

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

        public static Field ToField(this IXDescriptorProperty idfield)
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

        public static IXQuery ToIXQuery(this Document doc, string uniqueIdentifierField, IEnumerable<string> selectedFields)
        {
            IQueryable<IFieldable> fields = doc.GetFields().Where(p => p.Name != uniqueIdentifierField).AsQueryable();
            IXQuery iDoc = new IXQuery(uniqueIdentifierField);

            if (selectedFields != null && selectedFields.Count() > 0)
                fields = fields.Where(p => selectedFields.Contains(p.Name));

            fields.ToList().ForEach(f => iDoc.Results.Add(new IXQueryResult()
            {
                Name = f.Name,
                Value = (!f.IsBinary) ? f.StringValue : Convert.ToBase64String(f.GetBinaryValue()),
                IsEncoded = f.IsBinary
            }));

            return iDoc;
        }

        public static string UniqueIdentifierValue(this IXDescriptor descriptor)
        {
            IXDescriptorProperty unique = descriptor.Properties.FirstOrDefault(p => p.Name == descriptor.UniqueIdentifierField);
            return (unique.IsBinary) ? Convert.ToBase64String(unique.BinaryValue) : unique.StringValue;
        }

        public static Document ToDocument(this IXDescriptor descriptor, string uniqueValue=null)
        {
            Document doc = new Document();

            doc.Add(new Field(  DocumentUniqueIdentifierFieldName,
                                (uniqueValue==null) ? descriptor.UniqueIdentifierValue() : uniqueValue, 
                                Field.Store.YES, 
                                Field.Index.NOT_ANALYZED
                    ));
            
            foreach (IXDescriptorProperty f in descriptor.Properties.Where(p => p.Name != descriptor.UniqueIdentifierField))
                doc.Add(f.ToField());

            return doc;
        }
    }
}
