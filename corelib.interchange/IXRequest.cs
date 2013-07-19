using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace InfoStream.Metadata
{
    [Flags]
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
    public sealed class IXRequest
    {
        public string Query { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
        public IXRequestFlags Flags { get; set; }
        public IEnumerable<string> Fields { get; set; }

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
