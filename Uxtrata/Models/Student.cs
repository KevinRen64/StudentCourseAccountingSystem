using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Uxtrata.Models
{
    public class Student
    {
        [Key]
        public int StudentId { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }

        // One Student can enroll in many courses.
        public virtual ICollection<CourseSelection> CourseSelections { get; set; }
        // Each Payment record belongs to exactly one Student
        public virtual ICollection<Payment> Payments { get; set; }
    }
}