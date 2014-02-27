using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using TK.SPTools.CAMLBuilder.Expressions;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            Expression exp = Operator.Or(
                                    Operator.And(
                                        Criteria.Contains("fieldA", "valueForA"),
                                        Criteria.Contains("fieldB", "valueForB")
                                    ),

                                    Criteria.Contains("fieldC", "valueForC")
                                );
            
            // (A && B) || C =>

            //Expression exp2 =
            //        (Criteria.Contains("fieldA", "valueForA") & Criteria.Contains("fieldB", "valueForB")) | Criteria.Contains("fieldC", "valueForC");

            string c1 = exp.GetCAML();
            //string c2 = exp2.GetCAML();
        }
    }
}
