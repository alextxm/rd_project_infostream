using System;
using System.Collections.Generic;
using System.Text;

namespace TK.SPTools.CAMLBuilder.Expressions
{
    public sealed class Not : Operator
    {
        internal Not()
        {
        }

        internal Not(IEnumerable<Expression> expressions)
            : base(expressions)
        {
            
        }

        protected override string OpName
        {
            get { return "-"; }
        }
    }
}
