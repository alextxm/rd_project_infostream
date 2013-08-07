using System;
using System.Collections.Generic;
using System.Text;

namespace TK.SPTools.CAMLBuilder.Expressions
{
    public enum CriteriaType
    {
        Eq,
        //Neq,       
        BeginsWith,
        EndsWith,
        Contains,
        Fuzzy,
        Similar
    }

    public abstract class Criteria : Expression
    {
        protected string _fieldName;
        protected CriteriaType _criteriaType;

        protected Criteria()
        {
        }

        protected internal Criteria(string fieldName, CriteriaType criteriaType) : this()
        {
            _fieldName = fieldName;
            _criteriaType = criteriaType;
        }

        public string FieldName
        {
            get { return _fieldName; }
        }

        public CriteriaType CriteriaType
        {
            get { return _criteriaType; }
        }

        public abstract string Value
        {
            get;
            set;
        }

        protected string GetCriteriaSymbol()
        {
            switch (_criteriaType)
            {
                case CriteriaType.Eq:
                    return "Eq";
                //case CriteriaType.Neq:
                //    return "Neq";
                case CriteriaType.BeginsWith:
                    return "BeginsWith";
                case CriteriaType.EndsWith:
                    return "BeginsWith";
                case CriteriaType.Contains:
                    return "Contains";
                case CriteriaType.Fuzzy:
                    return "Fuzzy";
                case CriteriaType.Similar:
                    return "Similar";
                default:
                    throw new Exception("Unhandled CriteriaType: " + _criteriaType.ToString());
            }
        }

        //static builders
        public static Criteria Eq(string fieldName, string fieldType, string value)
        {
            return new ConditionCriteria(fieldName, value, CriteriaType.Eq);
        }

        //public static Criteria Neq(string fieldName, string fieldType, string value)
        //{
        //    return new ConditionCriteria(fieldName, fieldType, value, CriteriaType.Neq);
        //}

        public static Criteria BeginsWith(string fieldName, string value)
        {
            return new ConditionCriteria(fieldName, value, CriteriaType.BeginsWith);
        }

        public static Criteria EndsWith(string fieldName, string value)
        {
            return new ConditionCriteria(fieldName, value, CriteriaType.BeginsWith);
        }

        public static Criteria Contains(string fieldName, string value)
        {
            return new ConditionCriteria(fieldName, value, CriteriaType.Contains);
        }

        public static Criteria Fuzzy(string fieldName, string value)
        {
            return new ConditionCriteria(fieldName, value, CriteriaType.Fuzzy);
        }

        public static Criteria Similar(string fieldName, string value, double factor)
        {
            return new ConditionCriteria(fieldName, value, CriteriaType.Similar, factor);
        }

        //criteria operators
        //public static Criteria operator !(Criteria exp)
        //{
        //    if (exp is ConditionCriteria)
        //    {
        //        ConditionCriteria cexp = (ConditionCriteria)exp;

        //        if (exp._criteriaType == CriteriaType.Eq)
        //            return new ConditionCriteria(cexp.FieldName, cexp.FieldType, cexp.Value, CriteriaType.Neq);
        //        else if (exp._criteriaType == CriteriaType.Neq)
        //            return new ConditionCriteria(cexp.FieldName, cexp.FieldType, cexp.Value, CriteriaType.Eq);
        //    }
        //    else if (exp is SimpleCriteria)
        //    {
        //        SimpleCriteria cexp = (SimpleCriteria)exp;

        //        if (exp._criteriaType == CriteriaType.IsNull)
        //            return new SimpleCriteria(cexp.FieldName, CriteriaType.IsNotNull);
        //        else if (exp._criteriaType == CriteriaType.IsNotNull)
        //            return new SimpleCriteria(cexp.FieldName, CriteriaType.IsNull);
        //    }

        //    throw new Exception("Only Eq, Neq, IsNull, IsNotNull criterias can be negated.");
        //}
    }
}
