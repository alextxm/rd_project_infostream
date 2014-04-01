namespace testcf.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Demo1 : DbMigration
    {
        public override void Up()
        {
            DropIndex("demoes", "IX_Demo_AuxInfo1");
        }

        public override void Down()
        {
            CreateIndex("demoes", new string[] { "AuxInfo1" }, true, "IX_Demo_AuxInfo1");
        }
    }
}
