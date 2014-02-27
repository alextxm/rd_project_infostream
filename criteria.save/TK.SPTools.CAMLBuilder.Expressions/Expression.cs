using System;
using System.Collections.Generic;
using System.Text;

namespace TK.SPTools.CAMLBuilder.Expressions
{
    public abstract class Expression
    {
        internal Expression()
        {
        }

        public string GetCAML()
        {
            return GetCAMLInternal();
        }

        protected internal abstract string GetCAMLInternal();
        
        // NOT
        public static Operator operator !(Expression exp)
        {
            return Operator.Not(exp);
        }

        // OR
        public static Operator operator |(Expression exp1, Expression exp2)
        {
            return Operator.Or(exp1, exp2);
        }

        // AND
        public static Operator operator &(Expression exp1, Expression exp2)
        {
            return Operator.And(exp1, exp2);
        }
    }
}
