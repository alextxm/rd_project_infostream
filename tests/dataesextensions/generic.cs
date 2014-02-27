using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Nest;
using datatypes;

namespace dataesextensions
{
    public interface IElasticSearchTypeMapper
    {
        bool MapToElasticSearch(string indexname, IElasticClient client);
    }

    public abstract class ElasticSearchTypeMapper<T> : IElasticSearchTypeMapper
    {
        public ElasticSearchTypeMapper()
        {
        }

        public abstract bool MapToElasticSearch(string indexname, IElasticClient client);
    }
}
