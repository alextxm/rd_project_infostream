using System;
using System.Collections.Generic;
using System.Text;

namespace TK.SPTools.CAMLBuilder.Expressions
{
    public sealed class Or : Operator
    {
        internal Or()
        {
        }

        internal Or(IEnumerable<Expression> expressions)
            : base(expressions)
        {
            
        }

        protected override string OpName
        {
            get { return "OR"; }
        }
    }
}
