using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace Uxtrata.Models
{
    public class Course
    {
        [Key]
        public int CourseId { get; set; }
        public string CourseName { get; set; }
        public decimal Cost { get; set; }

        // many-to-many relationship between Student and Course.
        public virtual ICollection<CourseSelection> CourseSelections { get; set; }

    }
}