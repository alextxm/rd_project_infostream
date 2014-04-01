namespace testfs.Model.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class tfsIndices_v2_0 : DbMigration
    {
        public override void Up()
        {
            CreateIndex("dbo.Files", new string[] { "FilePathHash" }, true, "IX_Files_FilePathHash"); //  --> UNIQUE
            CreateIndex("dbo.Scans", new string[] { "ScanPathHash" }, false, "IX_Scans_ScanPathHash"); //  --> UNIQUE
            CreateIndex("dbo.Runs", new string[] { "CheckId" }, false, "IX_Runs_CheckId"); //  --> UNIQUE
        }
        
        public override void Down()
        {
            DropIndex("dbo.Files", "IX_Files_FilePathHash");
            DropIndex("dbo.Scans", "IX_Scans_ScanPathHash");
            DropIndex("dbo.Runs", "IX_Runs_CheckId");
        }
    }
}
