using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Migrations.History;
using System.Linq;
using System.Text;

namespace testfs.Model
{
    [DbConfigurationType(typeof(MySql.Data.Entity.MySqlEFConfiguration))]
    public class TfsContext : DbContext
    {
        public DbSet<File> Files { get; set; }
        public DbSet<Scan> Scans { get; set; }
        public DbSet<Run> Runs { get; set; }

        public TfsContext()
            : base("name=TfsContext")
        {
        }

        public TfsContext(string connectionString)
            : base(connectionString)
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {

        }
    }
}
