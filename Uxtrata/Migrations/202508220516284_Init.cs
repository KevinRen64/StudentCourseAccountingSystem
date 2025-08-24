namespace Uxtrata.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Init : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Courses",
                c => new
                    {
                        CourseId = c.Int(nullable: false, identity: true),
                        CourseName = c.String(unicode: false),
                        Cost = c.Decimal(nullable: false, precision: 10, scale: 2),
                    })
                .PrimaryKey(t => t.CourseId);
            
            CreateTable(
                "dbo.CourseSelections",
                c => new
                    {
                        CourseSelectionId = c.Int(nullable: false, identity: true),
                        StudentID = c.Int(nullable: false),
                        CourseID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.CourseSelectionId)
                .ForeignKey("dbo.Courses", t => t.CourseID, cascadeDelete: true)
                .ForeignKey("dbo.Students", t => t.StudentID, cascadeDelete: true)
                .Index(t => t.StudentID)
                .Index(t => t.CourseID);
            
            CreateTable(
                "dbo.Students",
                c => new
                    {
                        StudentId = c.Int(nullable: false, identity: true),
                        Name = c.String(unicode: false),
                        Age = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.StudentId);
            
            CreateTable(
                "dbo.Payments",
                c => new
                    {
                        PaymentId = c.Int(nullable: false, identity: true),
                        StudentId = c.Int(nullable: false),
                        Amount = c.Decimal(nullable: false, precision: 10, scale: 2),
                        PaidAt = c.DateTime(nullable: false, precision: 0),
                        Reference = c.String(unicode: false),
                    })
                .PrimaryKey(t => t.PaymentId)
                .ForeignKey("dbo.Students", t => t.StudentId, cascadeDelete: true)
                .Index(t => t.StudentId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Payments", "StudentId", "dbo.Students");
            DropForeignKey("dbo.CourseSelections", "StudentID", "dbo.Students");
            DropForeignKey("dbo.CourseSelections", "CourseID", "dbo.Courses");
            DropIndex("dbo.Payments", new[] { "StudentId" });
            DropIndex("dbo.CourseSelections", new[] { "CourseID" });
            DropIndex("dbo.CourseSelections", new[] { "StudentID" });
            DropTable("dbo.Payments");
            DropTable("dbo.Students");
            DropTable("dbo.CourseSelections");
            DropTable("dbo.Courses");
        }
    }
}
