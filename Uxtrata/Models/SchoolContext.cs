using MySql.Data.EntityFramework;
using System.Data.Entity;


namespace Uxtrata.Models
{

    // Tells EF to use MySQL provider instead of the default SQL Server one
    [DbConfigurationType(typeof(MySqlEFConfiguration))]

    public class SchoolContext : DbContext
    {
        public SchoolContext() : base("SchoolContext") { }

        // Tables in the database
        public DbSet<Student> Students { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<CourseSelection> CourseSelections { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<LedgerEntry> LedgerEntries { get; set; }

        // Override EF’s default model building to apply custom rules
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Ensure all decimals map to DECIMAL(10,2) in MySQL 
            modelBuilder.Properties<decimal>().Configure(c => c.HasPrecision(10, 2));

            base.OnModelCreating(modelBuilder);
        }

    }

}