using System;
using System.Collections.Generic;
using System.Text;

namespace TK.SPTools.CAMLBuilder.Expressions
{
    public sealed class And : Operator
    {
        internal And()
        {
        }

        internal And(IEnumerable<Expression> expressions)
            : base(expressions)
        {
            
        }

        protected override string OpName
        {
            get { return "AND"; }
        }
    }
}
