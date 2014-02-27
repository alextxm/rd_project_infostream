using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Nest;

namespace Nest.Extensions
{
    /// <summary>
    /// extends the NEST ElasticClient with custom fixes
    /// </summary>
    public class NestElasticClient : ElasticClient
    {
        public bool IsConnected
        {
            get
            {
                ConnectionStatus status = this.Raw.ClusterStateGet();
                if (status.Success != true && status.Error != null && status.Error.Type == Nest.ConnectionErrorType.Server)
                    return false;

                return true;
            }
        }

        public NestElasticClient(IConnectionSettings settings)
            : base(settings)
        {
        }

        public NestElasticClient(IConnectionSettings settings, IConnection connection)
            : base(settings, connection)
        {
        }

        /// <summary>
        /// Get the current mapping for T at the default index
        /// </summary>
        public new RootObjectMapping GetMapping<T>() where T : class
        {
            var index = this.Infer.IndexName<T>();
            return this.GetMapping<T>(index);
        }

        /// <summary>
        /// Get the current mapping for T at the specified index
        /// </summary>
        public new RootObjectMapping GetMapping<T>(string index) where T : class
        {
            string type = this.Infer.TypeName<T>();
            return this.GetMapping(index, type);
        }
        /// <summary>
        /// Get the current mapping for T at the default index
        /// </summary>
        public new RootObjectMapping GetMapping(Type t)
        {
            var index = this.Infer.IndexName(t);
            return this.GetMapping(t, index);
        }
        /// <summary>
        /// Get the current mapping for T at the specified index
        /// </summary>
        public new RootObjectMapping GetMapping(Type t, string index)
        {
            string type = this.Infer.TypeName(t);
            return this.GetMapping(index, type);
        }

        /// <summary>
        /// Get the current mapping for type at the specified index
        /// </summary>
        public new RootObjectMapping GetMapping(string index, string type)
        {
            string path = this.PathResolver.CreateIndexTypePath(index, type, "_mapping");

            ConnectionStatus status = this.Connection.GetSync(path);
            try
            {
                // -- alextxm: add support for ES 1.0-betaN, which adds indexname as root
                Newtonsoft.Json.Linq.JToken p = Newtonsoft.Json.Linq.JObject.Parse(status.Result).GetValue(index);
                var mappings = this.Serializer.Deserialize<IDictionary<string, RootObjectMapping>>((p == null) ? status.Result : p.ToString());
                // --

                if (status.Success)
                {
                    var mapping = mappings.First();
                    mapping.Value.Type = mapping.Key;

                    // -- alextxm: add support for the analyzer property
                    Newtonsoft.Json.Linq.JProperty analyzerProperty = ((Newtonsoft.Json.Linq.JObject.Parse(p.ToString()).First.First) as Newtonsoft.Json.Linq.JObject).Property("analyzer");
                    if (analyzerProperty != null)
                    {
                        mapping.Value.IndexAnalyzer = analyzerProperty.Value.ToString();
                        mapping.Value.SearchAnalyzer = analyzerProperty.Value.ToString();
                    }
                    // --

                    return mapping.Value;
                }
            }
            catch (Exception e)
            {
                //TODO LOG
            }
            return null;
        }
    }
}
