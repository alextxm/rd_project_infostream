using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace corelib.interchange.M2
{
    public enum IXDQResultStatus : int
    {
        [EnumMember]
        Success = 0,
        [EnumMember]
        NoData = 1,
        [EnumMember]
        Fail = 9,
        [EnumMember]
        ErrorQuerySyntax = 10,
        [EnumMember]
        ErrorInternal = 11
    }

    [ServiceKnownType(typeof(IXDQResultStatus))]
    [ServiceKnownType(typeof(IXDQ))] 
    public sealed class IXDQCollection
    {
        public int Count { get; set; }
        public int Start { get; set; }
        public int Take { get; set; }
        public IXDQResultStatus ResultStatus { get; set; }
        public IEnumerable<IXDQ> Elements { get; set; }
    }

    public sealed class IXDQResult
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public bool IsEncoded { get; set; }
    }

    public sealed class IXDQ
    {
        public sealed class Value
        {
            public string String { get; set; }
            public byte[] Binary { get; set; }
        }

        public float Score { get; set; }

        private string uniqueIdentifierField = null;
        public string UniqueIdentifierField
        {
            get { return uniqueIdentifierField; }
            set { uniqueIdentifierField = value; }
        }

        private List<IXDQResult> results = new List<IXDQResult>();
        public List<IXDQResult> Results
        {
            get { return results; }
            set { results = value; }
        }

        public Value this[string index]
        {
            get
            {
                if (results == null || results.Count < 1)
                    return null;

                IXDQResult val = results.FirstOrDefault(p => p.Name == index);
                
                if (val == null)
                    return null;
                else
                {
                    return new Value()
                                    {
                                        String = (val.IsEncoded) ? null : val.Value,
                                        Binary =(val.IsEncoded) ? Convert.FromBase64String(val.Value) : null
                                    };
                }
            }
        }

        public IXDQ(string uniqueIdentifierField)
        {
            if (String.IsNullOrEmpty(uniqueIdentifierField))
                throw new ArgumentNullException("uniqueIdentifierField");
        }

        public IXDQ(string uniqueIdentifierField, IEnumerable<IXDQResult> results)
        {
            if (results == null)
                throw new ArgumentNullException("results");

            if (String.IsNullOrEmpty(uniqueIdentifierField) || !results.Any(p => p.Name == uniqueIdentifierField))
                throw new ArgumentNullException("uniqueIdentifierField");

            this.results.AddRange(results);
        }

        public Value Get(string key)
        {
            return this[key];
        }

        public string GetString(string key)
        {
            IXDQResult f = results.FirstOrDefault(p => p.Name == key);
            return (f == null) ? null : f.Value;
        }

        public byte[] GetBinary(string key)
        {
            IXDQResult f = results.FirstOrDefault(p => p.Name == key);
            return (f == null) ? null : (f.IsEncoded) ? Convert.FromBase64String(f.Value) : null;
        }
    }
}
