using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace InfoStream.Metadata
{
    [Flags]
    [DataContract]
    public enum IXRequestFlags
    {
        [EnumMember]
        None = 0x00,
        [EnumMember]
        UseScore = 0x01
    }

    /// <summary>
    /// definizione di una richiesta (ricerca) all'indexer
    /// </summary>
    [ServiceKnownType(typeof(IXRequestFlags))]
    [DataContract]
    [Serializable]
    public sealed class IXRequest
    {
        [DataMember]
        public string Query { get; set; }
        [DataMember]
        public int Skip { get; set; }
        [DataMember]
        public int Take { get; set; }
        [DataMember]
        public IXRequestFlags Flags { get; set; }
        [DataMember]
        public IEnumerable<string> Fields { get; set; }

        public IXRequest()
        {
        }

        public IXRequest(string query, int skip, int take, IEnumerable<string> fields=null)
        {
            if (String.IsNullOrEmpty(query))
                throw new ArgumentNullException("query");

            Query = query;
            Skip = skip;
            Take = take;
            Fields = (fields == null) ? new string[] { } : fields;
            Flags = IXRequestFlags.None;
        }

        public IXRequest(string query, int skip, int take, IXRequestFlags flags, IEnumerable<string> fields = null)
        {
            if (String.IsNullOrEmpty(query))
                throw new ArgumentNullException("query");

            Query = query;
            Skip = skip;
            Take = take;
            Fields = (fields == null) ? new string[] { } : fields;
            Flags = flags;
        }
    }
}
