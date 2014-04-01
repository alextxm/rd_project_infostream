using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if !USE_PLAINEN
using Nest;

namespace test_data
{
    public partial class StaticDataFeed
    {
        public static bool MapToElasticSearch(string indexname, IElasticClient client)
        {
            IIndicesResponse result = client.MapFluent<StaticDataFeed>(m => m
                                .TypeName("staticdatafeed")
                                .Enabled()
                                .IndexName(indexname)
                                .IgnoreConflicts()
                                .IndexAnalyzer("standard")
                                .SearchAnalyzer("standard")
                                .DateDetection(true)
                                .NumericDetection(true)
                //MapFromAttributes() is shortcut to fill property mapping using the types' attributes and properties
                //Allows us to map the exceptions to the rule and be less verbose.
                                .MapFromAttributes()
                                //.IdField(p => p
                                //    .SetIndex("not_analyzed")
                                //    .SetPath("id")
                                //    .SetStored(true))
                                .Properties(props => props
                                    .Number(s => s
                                        .Name(n => n.id)
                                        .Type(NumberType.integer)
                                        .Index(NonStringIndexOption.not_analyzed)
                                        .Store(true))
                                    .Boolean(s => s
                                        .Name(n => n.attivo)
                                        .Index(NonStringIndexOption.not_analyzed)
                                        .Store(true))
                                    .Number(s => s
                                        .Name(n => n.anno)
                                        .Type(NumberType.integer)
                                        .Index(NonStringIndexOption.not_analyzed)
                                        .Store(true))
                                    .String(s => s
                                        .Name(n => n.codicemw)
                                        .Index(FieldIndexOption.not_analyzed)
                                        .Store(true))
                                    .Number(s => s
                                        .Name(n => n.durata)
                                        .Index(NonStringIndexOption.not_analyzed)
                                        .Store(true))
                                    .Date(s => s
                                        .Name(n => n.data_rilascio)
                                        .Index(NonStringIndexOption.not_analyzed)
                                        .Store(true))
                                    .Date(s => s
                                        .Name(n => n.data_inserimento)
                                        .Index(NonStringIndexOption.not_analyzed)
                                        .Store(true))
                                    )
                                );

            if (result == null || !result.OK || !result.IsValid)
                return false;

            return true;
        }
    }
}
#endif
