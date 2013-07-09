using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace corelib.interchange.M2
{
    public enum IXDQResult : int
    {
        Success = 0,
        Fail = 1,
        ErrorQuerySyntax = 10,
        ErrorInternal = 11,
        ErrorNoData = 12
    }

    [Flags]
    public enum IXDQOperationFlags : int
    {
        NoExtensions = 0x00,
        WithScore = 0x01
    }

    public sealed class IXDQCollection
    {
        public int Count { get; set; }
        public int Start { get; set; }
        public int Take { get; set; }
        public IXDQResult Result { get; set; }
        public IXDQOperationFlags Flags { get; set; }
        public IEnumerable<IXDQ> Elements { get; set; }
    }

    public sealed class IXDQ
    {
        public float Score { get; set; }

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

        public IXDQ(string uniqueIdentifierField)
        {
            if (String.IsNullOrEmpty(uniqueIdentifierField))
                throw new ArgumentNullException("uniqueIdentifierField");
        }

        public IXDQ(string uniqueIdentifierField, IEnumerable<InterchangeDocumentFieldInfo> fields)
        {
            if (fields == null)
                throw new ArgumentNullException("fields");

            if (String.IsNullOrEmpty(uniqueIdentifierField) || !fields.Any(p => p.Name == uniqueIdentifierField))
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
}
