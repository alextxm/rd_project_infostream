using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Migrations.History;
using System.Linq;
using System.Text;

namespace testcfss
{
    public class DCContext : DbContext
    {
        public DbSet<Demo> Demos { get; set; }

        public DCContext()
            : base("name=DCContext")
        {
        }

        public DCContext(string connectionString)
            : base(connectionString)
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
        }
    }
}
