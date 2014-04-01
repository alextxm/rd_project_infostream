using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace testcfss
{
    class Program
    {
        static void Main(string[] args)
        {
            using (DCContext context = new DCContext())
            {
                context.Demos.Any();
                context.SaveChanges();
            }
        }
    }
}
