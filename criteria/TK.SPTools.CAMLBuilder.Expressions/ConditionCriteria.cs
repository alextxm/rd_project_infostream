using System;
using System.Collections.Generic;
using System.Text;

namespace TK.SPTools.CAMLBuilder.Expressions
{
    public class ConditionCriteria : Criteria
    {
        protected string _value;

        private ConditionCriteria()
        {
        }

        internal ConditionCriteria(string fieldName, string value, CriteriaType criteriaType, object auxInfo=null)
            : base(fieldName, criteriaType)
        {
            _value = value;
        }

        public override string Value
        {
            get { return _value; }
            set { _value = value; }
        }

        protected internal override string GetCAMLInternal()
        {
            string str = @"[{1} {0} {2}]";
            return string.Format(str, GetCriteriaSymbol(), _fieldName, _value);
        }
    }
}
