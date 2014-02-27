using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Nest;
using datatypes;

namespace dataesextensions
{
    public class EmailMessageESMapper : ElasticSearchTypeMapper<EmailMessage>
    {
        public override bool MapToElasticSearch(string indexname, IElasticClient client)
        {
            IIndicesResponse result = client.Map<EmailMessage>(m => m
                                .Type("emailmessage")
                                .Index(indexname)
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
                                        .Name(n => n.Priority)
                                        .Type(NumberType.integer)
                                        .Index(NonStringIndexOption.not_analyzed)
                                        .Store(true))
                                    .Number(s => s
                                        .Name(n => n.Importance)
                                        .Type(NumberType.integer)
                                        .Index(NonStringIndexOption.not_analyzed)
                                        .Store(true))
                                    .Number(s => s
                                        .Name(n => n.Size)
                                        .Type(NumberType.integer)
                                        .Index(NonStringIndexOption.not_analyzed)
                                        .Store(true))
                                    .Date(s => s
                                        .Name(n => n.Sent)
                                        .Index(NonStringIndexOption.not_analyzed)
                                        .Store(true))
                                    .Date(s => s
                                        .Name(n => n.Received)
                                        .Index(NonStringIndexOption.not_analyzed)
                                        .Store(true))
                                    .String(s => s
                                        .Name(n => n.Filename)
                                        .Index(FieldIndexOption.not_analyzed)
                                        .Store(true))
                                    .String(s => s
                                        .Name(n => n.FullPathName)
                                        .Index(FieldIndexOption.not_analyzed)
                                        .Store(true))
                                    .String(s => s
                                        .Name(n => n.MessageID)
                                        .Index(FieldIndexOption.not_analyzed)
                                        .Store(true))
                                    .String(s => s
                                        .Name(n => n.ServerUID)
                                        .Index(FieldIndexOption.not_analyzed)
                                        .Store(true))
                                    )
                                );

            if (result == null || !result.IsValid || !result.Acknowledged)
                return false;

            return true;
        }
    }
}
