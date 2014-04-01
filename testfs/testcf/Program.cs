using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;

namespace testcf
{
    class Program
    {
        static void Main(string[] args)
        {
            using(DCContext context = new DCContext())
            {
                context.Demos.Any();
                context.SaveChanges();
            }
        }
    }
}
