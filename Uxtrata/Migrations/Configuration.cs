using System.Data.Entity.Migrations;

// Add these namespaces:
using MySql.Data.EntityFramework;


namespace Uxtrata.Migrations
{
    internal sealed class Configuration : DbMigrationsConfiguration<Uxtrata.Models.SchoolContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;

            // Tell EF6 how to generate SQL + history for MySQL
            SetSqlGenerator("MySql.Data.MySqlClient", new MySqlMigrationSqlGenerator());
            SetHistoryContextFactory("MySql.Data.MySqlClient",
                (conn, schema) => new MySqlHistoryContext(conn, schema));

            // Optional but helpful if you later rename namespaces
            ContextKey = "Uxtrata.Models.SchoolContext";
        }

        protected override void Seed(Uxtrata.Models.SchoolContext context)
        {
            // Example seeding (safe to leave empty for now)
            // context.Courses.AddOrUpdate(c => c.CourseName,
            //     new Course { CourseName = "Math 101", Cost = 1000m });
        }
    }
}
