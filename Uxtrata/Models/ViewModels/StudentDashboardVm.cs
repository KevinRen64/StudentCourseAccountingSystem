using System.Collections.Generic;
using Uxtrata.Models;

namespace Uxtrata.Models.ViewModels
{
    public class StudentDashboardVm
    {
        public Student Student { get; set; }
        public decimal Balance { get; set; }
        public List<CourseSelection> Enrollments { get; set; }
        public List<Payment> Payments { get; set; }
        public List<LedgerEntry> RecentEntries { get; set; }
    }
}
