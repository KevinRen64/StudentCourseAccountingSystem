namespace Uxtrata.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddAccounting : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Accounts",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Code = c.String(nullable: false, maxLength: 16, storeType: "nvarchar"),
                        Name = c.String(nullable: false, maxLength: 64, storeType: "nvarchar"),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.LedgerEntries",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TxnDate = c.DateTime(nullable: false, precision: 0),
                        AccountId = c.Int(nullable: false),
                        Debit = c.Decimal(nullable: false, precision: 10, scale: 2),
                        Credit = c.Decimal(nullable: false, precision: 10, scale: 2),
                        StudentId = c.Int(),
                        CourseId = c.Int(),
                        CourseSelectionId = c.Int(),
                        Description = c.String(maxLength: 256, storeType: "nvarchar"),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Accounts", t => t.AccountId, cascadeDelete: true)
                .ForeignKey("dbo.Courses", t => t.CourseId)
                .ForeignKey("dbo.CourseSelections", t => t.CourseSelectionId)
                .ForeignKey("dbo.Students", t => t.StudentId)
                .Index(t => t.AccountId)
                .Index(t => t.StudentId)
                .Index(t => t.CourseId)
                .Index(t => t.CourseSelectionId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.LedgerEntries", "StudentId", "dbo.Students");
            DropForeignKey("dbo.LedgerEntries", "CourseSelectionId", "dbo.CourseSelections");
            DropForeignKey("dbo.LedgerEntries", "CourseId", "dbo.Courses");
            DropForeignKey("dbo.LedgerEntries", "AccountId", "dbo.Accounts");
            DropIndex("dbo.LedgerEntries", new[] { "CourseSelectionId" });
            DropIndex("dbo.LedgerEntries", new[] { "CourseId" });
            DropIndex("dbo.LedgerEntries", new[] { "StudentId" });
            DropIndex("dbo.LedgerEntries", new[] { "AccountId" });
            DropTable("dbo.LedgerEntries");
            DropTable("dbo.Accounts");
        }
    }
}
