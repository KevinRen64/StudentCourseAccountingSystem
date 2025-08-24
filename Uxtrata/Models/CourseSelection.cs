using System.ComponentModel.DataAnnotations;


namespace Uxtrata.Models
{
    public class CourseSelection
    {
        [Key]
        public int CourseSelectionId { get; set; }


        // Foreign Key, link to a specific student
        [Required]
        public int StudentId { get; set; }

        // Foreign Key, link to a specific course
        [Required]
        public int CourseId { get; set; }


        // The Student entity for this enrollment
        public virtual Student Student { get; set; }

        // The Course entity for this enrollment
        public virtual Course Course { get; set; }
        
    }
}