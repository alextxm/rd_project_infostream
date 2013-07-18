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

        public static IXQuery ToIXQuery(this Document doc, string uniqueIdentifierField, IEnumerable<string> selectedFields, float? score=null)
        {
            IQueryable<IFieldable> fields = doc.GetFields().Where(p => p.Name != uniqueIdentifierField).AsQueryable();
            IXQuery iDoc = new IXQuery(uniqueIdentifierField);

            if (score != null)
                iDoc.Score = (float)score;

            if (selectedFields != null && selectedFields.Count() > 0)
                fields = fields.Where(p => selectedFields.Contains(p.Name));

            fields.ToList().ForEach(f => iDoc.records.Add(new IXQueryResult()
                                                                {
                                                                    Name = f.Name,
                                                                    Value = (!f.IsBinary) ? f.StringValue : Convert.ToBase64String(f.GetBinaryValue()),
                                                                    IsEncoded = f.IsBinary
                                                                }));

            return iDoc;
        }

        public static Document ToDocument(this IXDescriptor descriptor, string uniqueIdentifierField)
        {
            Document doc = new Document();

            bool uq = false;
            foreach (IXDescriptorProperty p in descriptor.Properties)
            {
                if ((p.Flags & FieldFlags.UNIQUEID) == FieldFlags.UNIQUEID)
                {
                    if (uq)
                        throw new InvalidOperationException("UNIQUEID not found");
                    else
                    {
                        doc.Add(new Field(uniqueIdentifierField,
                                            p.Name,
                                            Field.Store.YES,
                                            Field.Index.NOT_ANALYZED
                                ));

                        uq = true;
                    }
                }

                doc.Add(p.ToField());
            }

            return doc;
        }
    }
}
