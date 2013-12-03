using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.ServiceModel;


namespace InfoStream.Metadata
{
    [DataContract]
    public enum IXQueryStatus : int
    {
        [EnumMember]
        Success = 0,
        [EnumMember]
        NoData = 1,
        [EnumMember]
        NoSetup = 8,
        [EnumMember]
        Fail = 9,
        [EnumMember]
        ErrorQuerySyntax = 10,
        [EnumMember]
        ErrorInternal = 11
    }

    /// <summary>
    /// risposta ad una richiesta fatta all'indexer via IXRequest
    /// </summary>
    [ServiceKnownType(typeof(IXQueryStatus))]
    [ServiceKnownType(typeof(IXQuery))] 
    [DataContract]
    [Serializable]
    public sealed class IXQueryCollection
    {
        [DataMember]
        public int Count { get; set; }
        [DataMember]
        public int Start { get; set; }
        [DataMember]
        public int Take { get; set; }
        [DataMember]
        public IXQueryStatus Status { get; set; }
        [DataMember]
        public IEnumerable<IXQuery> Results { get; set; }
    }

    [DataContract]
    [Serializable]
    internal sealed class IXQueryResult
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Value { get; set; }
        [DataMember]
        public bool IsEncoded { get; set; }
    }

    [DataContract]
    [Serializable]
    public sealed class IXQuery
    {
        [DataContract]
        [Serializable]
        public sealed class Value
        {
            [DataMember]
            public string Name { get; set; }
            [DataMember]
            public string String { get; set; }
            [DataMember]
            public byte[] Binary { get; set; }
        }

        [DataMember]
        public float Score { get; set; }

        private string uniqueIdentifierField = null;
        public string UniqueIdentifierField
        {
            get { return uniqueIdentifierField; }
            set { uniqueIdentifierField = value; }
        }
        private string uniqueIdentifierField = null;
        [DataMember]
        public string UniqueIdentifierField
        {
            get { return uniqueIdentifierField; }
            set { uniqueIdentifierField = value; }
        }

        [DataMember(Name = "Records")]
        internal List<IXQueryResult> records = new List<IXQueryResult>();
        public List<Value> Records
        {
            get
            {
                return (records==null || records.Count<1) ? new List<Value>() : records.ConvertAll<Value>(p => new Value() { Name = p.Name, String = ((p.IsEncoded) ? null : p.Value), Binary = ((p.IsEncoded) ? Convert.FromBase64String(p.Value) : null) });
            }
        }

        [DataMember]
        public Value this[string index]
        {
            get
            {
                if (records == null || records.Count < 1)
                    return null;

                IXQueryResult val = records.FirstOrDefault(p => p.Name == index);
                
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

        internal IXQuery(string uniqueIdentifierField)
        {
            if (String.IsNullOrEmpty(uniqueIdentifierField))
                throw new ArgumentNullException("uniqueIdentifierField");
        }

        internal IXQuery(string uniqueIdentifierField, IEnumerable<IXQueryResult> results)
        {
            if (results == null)
                throw new ArgumentNullException("results");

            if (String.IsNullOrEmpty(uniqueIdentifierField) || !results.Any(p => p.Name == uniqueIdentifierField))
                throw new ArgumentNullException("uniqueIdentifierField");

            this.records.AddRange(results);
        }

        public Value Get(string key)
        {
            return this[key];
        }

        public string GetString(string key)
        {
            IXQueryResult f = records.FirstOrDefault(p => p.Name == key);
            return (f == null) ? null : f.Value;
        }

        public byte[] GetBinary(string key)
        {
            IXQueryResult f = records.FirstOrDefault(p => p.Name == key);
            return (f == null) ? null : (f.IsEncoded) ? Convert.FromBase64String(f.Value) : null;
        }
    }
}
