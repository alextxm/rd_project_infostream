using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace datatypes
{
    public class DemoClass
    {
        //[ElasticProperty(Index = FieldIndexOption.not_analyzed, Store=true)]
        public int Value { get; set; }
        public string FieldA { get; set; }
        public string FieldB { get; set; }
    }
}
