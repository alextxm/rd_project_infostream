using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Nest;
using datatypes;

namespace dataesextensions
{
    public class DemoClassESMapper : ElasticSearchTypeMapper<DemoClass>
    {
        public override bool MapToElasticSearch(string indexname, IElasticClient client)
        {
            IIndicesResponse result = client.Map<DemoClass>(m => m
                                        .Type("mydemoclass")
                                        .Index(indexname)
                                        .IgnoreConflicts()
                                        .IndexAnalyzer("standard")
                                        .SearchAnalyzer("standard")
                                        .DateDetection(true)
                                        .NumericDetection(true)
                                        .Properties(props => props
                                            .Number(s => s
                                                .Name(n => n.Value)
                                                .Type(NumberType.integer)
                                                .Index(NonStringIndexOption.not_analyzed)
                                                .Store(true))
                                            )
                                        );

            if (result == null || !result.IsValid || !result.Acknowledged)
                return false;

            return true;
        }
    }
}
