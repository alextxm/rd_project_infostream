#if USE_EF_DATAFEED
using System;
using System.Data.Entity;
using System.Data.Entity.Migrations;

namespace ISClient
{
    public partial class DataFeedContext : DbContext
    {
        public DbSet<StaticDataFeed> DataFeed { get; set; }

        private string lastError = String.Empty;
        public string LastError
        {
            get { return lastError; }
        }

        public DataFeedContext()
            : base("name=DataFeedContext")
        {
            //Database.SetInitializer<DataFeedContext>(null);
        }

        public bool Save()
        {
            try
            {
                this.SaveChanges();
                this.lastError = String.Empty;
                return true;
            }
            catch (Exception ex)
            {
                this.lastError = String.Format("{0} {1}", ex.Message, (ex.InnerException != null) ? ex.InnerException.Message : String.Empty);
            }

            return false;
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {

        }
    }
}
#endif
